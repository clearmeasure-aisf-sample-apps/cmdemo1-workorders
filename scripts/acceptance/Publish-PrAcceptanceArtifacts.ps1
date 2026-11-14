<#
.SYNOPSIS
  Scrubs, uploads, and/or sticky-comments PR acceptance videos and traces.

.DESCRIPTION
  Item 16 / #1671. Uploads only to private Azure Blob Storage (item 4 stack containers
  `videos` and `traces`) using Azure CLI `--auth-mode login` after OIDC / managed identity
  sign-in. Never uses account keys, connection strings, or SAS.

  Modes:
  - Upload: scrub selected traces, upload videos/traces (call from shard runners that hold files).
  - Comment: build/update sticky PR comment from uploaded blob paths (after the PR environment check).
  - All: Upload then Comment (local/integration helper).

  Does not publish videos/traces as GitHub Actions artifacts. Does not loosen gates.

.PARAMETER Mode
  Upload | Comment | All

.PARAMETER StorageAccountName
  Private account from item 4 pool stack (vars.POOL_ARTIFACTS_STORAGE_ACCOUNT). Required for Upload.

.PARAMETER RunId
  GitHub Actions run id (blob path prefix).

.PARAMETER TrxDirectory / TraceDirectory / VideoDirectory
  Local result folders for selection + upload.

.PARAMETER ChangedRazorPathsFile
  Optional file of changed Razor paths for changed-UI selection.

.PARAMETER ManifestPath
  JSON manifest written on Upload and read on Comment (blob paths). When Comment runs without
  a local manifest, lists blobs under the run prefix via az (auth-mode login).

.PARAMETER PullRequestNumber
  PR number for the sticky comment (Comment/All).

.PARAMETER Repository
  owner/repo for gh.

.PARAMETER DryRun
  Scrub and write manifest / comment body without az upload or gh comment.

.PARAMETER SelfTest
  Runs Publish-PrAcceptanceArtifacts.Tests.ps1.
#>
[CmdletBinding()]
param(
    [ValidateSet('Upload', 'Comment', 'All')]
    [string]$Mode = 'All',

    [string]$StorageAccountName = $env:POOL_ARTIFACTS_STORAGE_ACCOUNT,
    [string]$RunId = $env:GITHUB_RUN_ID,
    [string]$TrxDirectory = '',
    [string]$TraceDirectory = '',
    [string]$VideoDirectory = '',
    [string]$ChangedRazorPathsFile = '',
    [string]$ManifestPath = '',
    [string]$PullRequestNumber = $env:PR_NUMBER,
    [string]$Repository = $env:GITHUB_REPOSITORY,
    [string]$RunUrl = '',
    [switch]$DryRun,
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

if ($SelfTest) {
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Publish-PrAcceptanceArtifacts.Tests.ps1')
    exit $LASTEXITCODE
}

. (Join-Path $PSScriptRoot 'SecretHeaders.ps1')

function Assert-NoSecretMaterialInEnvDump {
    # Refuse to print secret values; callers must never echo headers.
    $forbidden = @('AZURE_STORAGE_KEY', 'AZURE_STORAGE_SAS_TOKEN', 'ACCOUNT_KEY', 'SAS_TOKEN')
    foreach ($name in $forbidden) {
        if (-not [string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($name))) {
            throw "Blocker: environment defines $name. Issue #1671 forbids keys/SAS; use OIDC --auth-mode login only."
        }
    }
}

function Invoke-AzBlobUpload {
    param(
        [string]$Account,
        [string]$Container,
        [string]$BlobName,
        [string]$FilePath
    )
    Assert-NoSecretMaterialInEnvDump
    & az storage blob upload `
        --account-name $Account `
        --container-name $Container `
        --name $BlobName `
        --file $FilePath `
        --auth-mode login `
        --overwrite true `
        --only-show-errors
    if ($LASTEXITCODE -ne 0) {
        throw "az storage blob upload failed for $Container/$BlobName (exit $LASTEXITCODE). Ensure the OIDC identity has Storage Blob Data Contributor on the item 4 artifacts account."
    }
}

function Get-Selection {
    $selArgs = @{
        AsJson = $true
    }
    if ($TrxDirectory) { $selArgs.TrxDirectory = $TrxDirectory }
    if ($TraceDirectory) { $selArgs.TraceDirectory = $TraceDirectory }
    if ($VideoDirectory) { $selArgs.VideoDirectory = $VideoDirectory }
    if ($ChangedRazorPathsFile) { $selArgs.ChangedRazorPathsFile = $ChangedRazorPathsFile }
    $json = & (Join-Path $PSScriptRoot 'Get-PrAcceptanceArtifactSelection.ps1') @selArgs
    if ([string]::IsNullOrWhiteSpace($json)) { return @() }
    $parsed = $json | ConvertFrom-Json
    if ($null -eq $parsed) { return @() }
    if ($parsed -is [System.Array]) { return @($parsed) }
    return @($parsed)
}

function Invoke-Upload {
    if ([string]::IsNullOrWhiteSpace($StorageAccountName) -and -not $DryRun) {
        throw 'Blocker: StorageAccountName / POOL_ARTIFACTS_STORAGE_ACCOUNT is empty. Set the GitHub Actions variable to the item 4 pool artifacts account name (not a secret). Live account comes from cmdemo1-system infra/pool/modules/artifacts.bicep after apply.'
    }
    if ([string]::IsNullOrWhiteSpace($RunId)) {
        $RunId = 'local'
    }

    $items = @(Get-Selection)
    $manifest = [System.Collections.Generic.List[object]]::new()
    $scrubScript = Join-Path $PSScriptRoot 'Scrub-PlaywrightTraceSecrets.ps1'

    foreach ($item in $items) {
        $safeName = ([string]$item.TestName) -replace '[^A-Za-z0-9._-]', '_'
        $videoBlob = $null
        $traceBlob = $null

        if ($item.TracePath -and (Test-Path -LiteralPath $item.TracePath)) {
            $scrubbed = Join-Path ([System.IO.Path]::GetTempPath()) ("scrubbed-$safeName.zip")
            & pwsh -NoProfile -File $scrubScript -Path $item.TracePath -Destination $scrubbed
            # Verify scrub: secret header value must not remain.
            $verifyRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("verify-" + [guid]::NewGuid().ToString('N'))
            New-Item -ItemType Directory -Force -Path $verifyRoot | Out-Null
            try {
                Expand-Archive -LiteralPath $scrubbed -DestinationPath $verifyRoot -Force
                $joined = Get-ChildItem -LiteralPath $verifyRoot -Recurse -File |
                    Where-Object { $_.Extension -in '.trace', '.network', '.json', '.txt' } |
                    ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw } |
                    Out-String
                foreach ($headerName in (Get-PrAcceptanceSecretHeaderNames)) {
                    # Allow the header *name* but not a non-redacted value pattern for test-support secret constant.
                }
                if ($joined -match 'acceptance-test-support-secret') {
                    throw "Scrub failed: plaintext test-support secret still present in $scrubbed"
                }
            }
            finally {
                Remove-Item -LiteralPath $verifyRoot -Recurse -Force -ErrorAction SilentlyContinue
            }

            $traceBlob = "traces/$RunId/$safeName.zip"
            if (-not $DryRun) {
                Invoke-AzBlobUpload -Account $StorageAccountName -Container 'traces' -BlobName "$RunId/$safeName.zip" -FilePath $scrubbed
            }
            Remove-Item -LiteralPath $scrubbed -Force -ErrorAction SilentlyContinue
        }

        if ($item.VideoPath -and (Test-Path -LiteralPath $item.VideoPath)) {
            $videoBlob = "videos/$RunId/$safeName.webm"
            if (-not $DryRun) {
                $ext = [System.IO.Path]::GetExtension($item.VideoPath)
                if ([string]::IsNullOrWhiteSpace($ext)) { $ext = '.webm' }
                Invoke-AzBlobUpload -Account $StorageAccountName -Container 'videos' -BlobName "$RunId/$safeName$ext" -FilePath $item.VideoPath
                $videoBlob = "videos/$RunId/$safeName$ext"
            }
        }

        if ($videoBlob -or $traceBlob) {
            $manifest.Add([pscustomobject]@{
                TestName  = [string]$item.TestName
                Reason    = [string]$item.Reason
                VideoBlob = $videoBlob
                TraceBlob = $traceBlob
            })
        }
    }

    if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
        $ManifestPath = Join-Path ([System.IO.Path]::GetTempPath()) "pr-acceptance-artifacts-$RunId.json"
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ManifestPath
    Write-Host "Wrote manifest $ManifestPath ($($manifest.Count) item(s))"
    return $ManifestPath
}

function Invoke-Comment {
    param([string]$PathToManifest)

    $items = @()
    if (-not [string]::IsNullOrWhiteSpace($PathToManifest) -and (Test-Path -LiteralPath $PathToManifest)) {
        $parsed = Get-Content -LiteralPath $PathToManifest -Raw | ConvertFrom-Json
        if ($parsed -is [System.Array]) { $items = @($parsed) }
        elseif ($null -ne $parsed) { $items = @($parsed) }
    }
    elseif (-not $DryRun -and $StorageAccountName -and $RunId) {
        Assert-NoSecretMaterialInEnvDump
        $listed = & az storage blob list --account-name $StorageAccountName --container-name videos --prefix "$RunId/" --auth-mode login -o json 2>$null
        if ($LASTEXITCODE -eq 0 -and $listed) {
            foreach ($blob in ($listed | ConvertFrom-Json)) {
                $base = [System.IO.Path]::GetFileNameWithoutExtension($blob.name)
                $items += [pscustomobject]@{
                    TestName  = $base
                    Reason    = 'uploaded'
                    VideoBlob = "videos/$($blob.name)"
                    TraceBlob = "traces/$RunId/$base.zip"
                }
            }
        }
    }

    $bodyFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sticky-pr-artifacts-" + [guid]::NewGuid().ToString('N') + '.md')
    & (Join-Path $PSScriptRoot 'Write-StickyPrArtifactComment.ps1') `
        -Items $items `
        -StorageAccountName $StorageAccountName `
        -RunUrl $RunUrl `
        -BodyPath $bodyFile | Out-Null

    if ($DryRun) {
        Write-Host "DryRun comment body at $bodyFile"
        Get-Content -LiteralPath $bodyFile | Write-Host
        return
    }

    if ([string]::IsNullOrWhiteSpace($PullRequestNumber) -or [string]::IsNullOrWhiteSpace($Repository)) {
        throw 'PullRequestNumber and Repository are required for Comment mode (or set PR_NUMBER and GITHUB_REPOSITORY).'
    }

    $marker = '<!-- pr-acceptance-artifacts -->'
    $existingJson = & gh api "repos/$Repository/issues/$PullRequestNumber/comments" --paginate 2>$null
    $existingId = $null
    if ($LASTEXITCODE -eq 0 -and $existingJson) {
        $existingId = $existingJson | ConvertFrom-Json |
            Where-Object { $_.body -like "*$marker*" } |
            Select-Object -First 1 -ExpandProperty id
    }

    $body = Get-Content -LiteralPath $bodyFile -Raw
    if ($existingId) {
        $body | & gh api "repos/$Repository/issues/comments/$existingId" -X PATCH -F body=@-
    }
    else {
        $body | & gh api "repos/$Repository/issues/$PullRequestNumber/comments" -X POST -F body=@-
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to upsert sticky PR comment (exit $LASTEXITCODE)"
    }
    Write-Host "Sticky PR comment updated on #$PullRequestNumber"
}

Assert-NoSecretMaterialInEnvDump

$manifestForComment = $ManifestPath
if ($Mode -eq 'Upload' -or $Mode -eq 'All') {
    $manifestForComment = Invoke-Upload
}
if ($Mode -eq 'Comment' -or $Mode -eq 'All') {
    Invoke-Comment -PathToManifest $manifestForComment
}
