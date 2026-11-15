#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Write-StickyPrArtifactComment.ps1'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

$bodyFile = Join-Path ([System.IO.Path]::GetTempPath()) ("sticky-test-" + [guid]::NewGuid().ToString('N') + '.md')
try {
    $inner = @"
`$items = @(
    [pscustomobject]@{
        TestName  = 'ClearMeasure.Bootcamp.AcceptanceTests.App.LandingPageTests.Should_Load'
        Reason    = 'failed'
        VideoBlob = 'videos/123/LandingPageTests.Should_Load.webm'
        TraceBlob = 'traces/123/LandingPageTests.Should_Load.zip'
    }
)
& '$scriptPath' -StorageAccountName 'stcmdemo1poolart' -RunUrl 'https://example.test/run' -BodyPath '$bodyFile' -Items `$items
"@
    & pwsh -NoProfile -Command $inner
    Assert-True ($LASTEXITCODE -eq 0) "Write-Sticky exit $LASTEXITCODE"
    $text = Get-Content -LiteralPath $bodyFile -Raw
    Assert-True ($text -match '<!-- pr-acceptance-artifacts -->') 'Sticky marker missing'
    Assert-True ($text -match 'No SAS') 'Must state no SAS'
    Assert-True ($text -match 'stcmdemo1poolart') 'Account name missing'
    Assert-True ($text -match 'LandingPageTests') 'Test name missing'
    Assert-True ($text -match 'auth-mode login') 'OIDC download example missing'
    Assert-True ($text -notmatch 'sig=') 'Must not embed SAS query'
    Assert-True ($text -notmatch 'SharedAccessSignature') 'Must not mention SAS credential material'

    $emptyBody = (& pwsh -NoProfile -Command "& '$scriptPath'") -join "`n"
    Assert-True ($emptyBody -match 'No failed or changed-UI') 'Empty run message missing'

    Write-Host 'Write-StickyPrArtifactComment.Tests.ps1 passed'
}
finally {
    Remove-Item -LiteralPath $bodyFile -Force -ErrorAction SilentlyContinue
}
