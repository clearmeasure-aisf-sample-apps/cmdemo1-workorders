<#
.SYNOPSIS
  Shared Playwright trace scrub + verify helpers for #1671.

.DESCRIPTION
  Dot-sourced by Scrub-PlaywrightTraceSecrets.ps1 and Publish-PrAcceptanceArtifacts.ps1.
  Requires SecretHeaders.ps1 to be loaded first.
#>

function Test-IsTextTraceEntry {
    param([string]$EntryName)
    $n = $EntryName.ToLowerInvariant()
    return ($n.EndsWith('.trace') -or $n.EndsWith('.network') -or $n.EndsWith('.json') -or $n.EndsWith('.txt') -or $n.EndsWith('.md'))
}

function Get-JsonPropertyIgnoreCase {
    param($Object, [string]$Name)
    if ($null -eq $Object) { return $null }
    $prop = $Object.PSObject.Properties | Where-Object {
        [string]::Equals($_.Name, $Name, [StringComparison]::OrdinalIgnoreCase)
    } | Select-Object -First 1
    if ($null -eq $prop) { return $null }
    return $prop.Value
}

function Set-JsonPropertyIgnoreCase {
    param($Object, [string]$Name, $Value)
    $prop = $Object.PSObject.Properties | Where-Object {
        [string]::Equals($_.Name, $Name, [StringComparison]::OrdinalIgnoreCase)
    } | Select-Object -First 1
    if ($null -ne $prop) {
        $prop.Value = $Value
    }
}

function Test-IsHeaderContainerName {
    param([string]$Name)
    return (
        [string]::Equals($Name, 'headers', [StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($Name, 'requestHeaders', [StringComparison]::OrdinalIgnoreCase) -or
        [string]::Equals($Name, 'responseHeaders', [StringComparison]::OrdinalIgnoreCase)
    )
}

function Test-IsCookieContainerName {
    param([string]$Name)
    return [string]::Equals($Name, 'cookies', [StringComparison]::OrdinalIgnoreCase)
}

function Scrub-NameValueObject {
    param($Obj, [bool]$ForceRedactValue)
    $name = Get-JsonPropertyIgnoreCase -Object $Obj -Name 'name'
    $value = Get-JsonPropertyIgnoreCase -Object $Obj -Name 'value'
    if ($null -eq $name -or $null -eq $value) { return }
    $nameText = [string]$name
    $valueText = [string]$value
    if ($ForceRedactValue -or (Test-PrAcceptanceValueLooksSecret -HeaderName $nameText -HeaderValue $valueText)) {
        Set-JsonPropertyIgnoreCase -Object $Obj -Name 'value' -Value (Get-PrAcceptanceRedactedValue)
    }
}

function Scrub-HeaderMap {
    param($Map)
    if ($null -eq $Map) { return }
    foreach ($prop in @($Map.PSObject.Properties)) {
        if ($null -eq $prop) { continue }
        $headerName = [string]$prop.Name
        $headerValue = if ($null -eq $prop.Value) { '' } else { [string]$prop.Value }
        if (Test-PrAcceptanceValueLooksSecret -HeaderName $headerName -HeaderValue $headerValue) {
            $prop.Value = Get-PrAcceptanceRedactedValue
        }
    }
}

function Scrub-HeaderPairArray {
    param($Pair)
    # ["Header-Name", "secret-value"]
    if ($null -eq $Pair) { return }
    $items = @($Pair)
    if ($items.Count -lt 2) { return }
    $headerName = [string]$items[0]
    $headerValue = [string]$items[1]
    if (Test-PrAcceptanceValueLooksSecret -HeaderName $headerName -HeaderValue $headerValue) {
        # PSCustomObject arrays from ConvertFrom-Json are Object[]; mutate via index when possible.
        if ($Pair -is [System.Array] -and $Pair.Length -ge 2) {
            $Pair[1] = Get-PrAcceptanceRedactedValue
        }
    }
}

function Scrub-HeaderContainer {
    param($Container)
    if ($null -eq $Container) { return }

    if ($Container -is [System.Array] -or ($Container -is [System.Collections.IEnumerable] -and $Container -isnot [string])) {
        $list = @($Container)
        if ($list.Count -eq 0) { return }
        $first = $list[0]
        if ($first -is [System.Array] -or ($first -is [System.Collections.IList] -and $first -isnot [string])) {
            foreach ($pair in $list) {
                Scrub-HeaderPairArray -Pair $pair
            }
            return
        }
        if ($null -ne $first -and $null -ne (Get-JsonPropertyIgnoreCase -Object $first -Name 'name')) {
            foreach ($obj in $list) {
                Scrub-NameValueObject -Obj $obj -ForceRedactValue:$false
            }
            return
        }
    }

    # Object map: { "Authorization": "Bearer x", ... }
    if ($null -ne $Container.PSObject -and $Container.PSObject.Properties.Count -gt 0) {
        Scrub-HeaderMap -Map $Container
    }
}

function Scrub-CookieContainer {
    param($Container)
    if ($null -eq $Container) { return }
    foreach ($obj in @($Container)) {
        if ($null -eq $obj) { continue }
        # Always redact cookie values (session material); same as Cookie / Set-Cookie headers.
        Scrub-NameValueObject -Obj $obj -ForceRedactValue:$true
    }
}

function Scrub-JsonObject {
    param($Node)
    if ($null -eq $Node) { return }

    if ($Node -is [System.Array] -or ($Node -is [System.Collections.IEnumerable] -and $Node -isnot [string] -and $Node -isnot [System.Management.Automation.PSCustomObject])) {
        foreach ($item in @($Node)) {
            Scrub-JsonObject -Node $item
        }
        return
    }

    if ($null -eq $Node.PSObject) { return }

    foreach ($prop in @($Node.PSObject.Properties)) {
        if ($null -eq $prop) { continue }
        $name = [string]$prop.Name
        $value = $prop.Value
        if (Test-IsHeaderContainerName -Name $name) {
            Scrub-HeaderContainer -Container $value
            continue
        }
        if (Test-IsCookieContainerName -Name $name) {
            Scrub-CookieContainer -Container $value
            continue
        }
        Scrub-JsonObject -Node $value
    }
}

function ConvertTo-PrAcceptanceJson {
    param($Object)
    # Depth high enough for Playwright resource snapshots; Compress keeps JSONL compact.
    return ($Object | ConvertTo-Json -Depth 100 -Compress)
}

function Scrub-JsonText {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) {
        return (Protect-PrAcceptanceSecretText -Text $Text)
    }

    # Whole-document JSON (pretty-printed / split across lines).
    try {
        $doc = $Text | ConvertFrom-Json -Depth 100 -ErrorAction Stop
        Scrub-JsonObject -Node $doc
        $out = ConvertTo-PrAcceptanceJson -Object $doc
        return (Protect-PrAcceptanceSecretText -Text $out)
    }
    catch {
        # Fall through to JSONL / line scrub.
    }

    # PowerShell -split treats a negative count as its absolute value (so -1 => 1 substring).
    # Use Regex.Split to get every line of JSONL.
    $lines = [regex]::Split($Text, '\r?\n')
    $outLines = New-Object System.Collections.Generic.List[string]
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            [void]$outLines.Add($line)
            continue
        }
        try {
            $obj = ConvertFrom-Json -InputObject $line -Depth 100 -ErrorAction Stop
            Scrub-JsonObject -Node $obj
            [void]$outLines.Add((Protect-PrAcceptanceSecretText -Text (ConvertTo-PrAcceptanceJson -Object $obj)))
        }
        catch {
            [void]$outLines.Add((Protect-PrAcceptanceSecretText -Text $line))
        }
    }
    $joined = [string]::Join("`n", $outLines)
    if ($Text.EndsWith("`n") -or $Text.EndsWith("`r`n")) {
        if (-not $joined.EndsWith("`n")) { $joined += "`n" }
    }
    return $joined
}

function Invoke-ScrubTraceZip {
    param(
        [Parameter(Mandatory = $true)][string]$SourceZip,
        [Parameter(Mandatory = $true)][string]$OutputZip
    )

    if (-not (Test-Path -LiteralPath $SourceZip)) {
        throw "Trace zip not found: $SourceZip"
    }

    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("scrub-trace-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
    try {
        Expand-Archive -LiteralPath $SourceZip -DestinationPath $tempRoot -Force
        Get-ChildItem -LiteralPath $tempRoot -Recurse -File | ForEach-Object {
            if (-not (Test-IsTextTraceEntry -EntryName $_.Name)) {
                return
            }
            $text = [System.IO.File]::ReadAllText($_.FullName)
            $scrubbed = Scrub-JsonText -Text $text
            [System.IO.File]::WriteAllText($_.FullName, $scrubbed)
        }

        $outDir = Split-Path -Parent $OutputZip
        if (-not [string]::IsNullOrWhiteSpace($outDir) -and -not (Test-Path -LiteralPath $outDir)) {
            New-Item -ItemType Directory -Force -Path $outDir | Out-Null
        }
        if (Test-Path -LiteralPath $OutputZip) {
            Remove-Item -LiteralPath $OutputZip -Force
        }
        Compress-Archive -Path (Join-Path $tempRoot '*') -DestinationPath $OutputZip -Force
    }
    finally {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Assert-TraceZipScrubbed {
    <#
    .SYNOPSIS
      Fails if any denylisted header value or live secret substring remains in a scrubbed zip.
    #>
    param([Parameter(Mandatory = $true)][string]$ZipPath)

    $verifyRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("assert-scrub-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $verifyRoot | Out-Null
    try {
        Expand-Archive -LiteralPath $ZipPath -DestinationPath $verifyRoot -Force
        $files = Get-ChildItem -LiteralPath $verifyRoot -Recurse -File |
            Where-Object { Test-IsTextTraceEntry -EntryName $_.Name }
        foreach ($file in $files) {
            $text = [System.IO.File]::ReadAllText($file.FullName)
            Assert-TraceTextScrubbed -Text $text -SourceName $file.Name
        }
    }
    finally {
        Remove-Item -LiteralPath $verifyRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Assert-TraceTextScrubbed {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text,
        [string]$SourceName = 'trace'
    )

    foreach ($secret in (Get-PrAcceptanceSecretSubstrings)) {
        if ([string]::IsNullOrEmpty($secret)) { continue }
        if ($Text.Contains($secret, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Scrub failed: denylisted secret substring still present in $SourceName"
        }
    }

    # Walk JSON shapes when possible and ensure denylisted header / cookie values are redacted.
    $nodes = New-Object System.Collections.Generic.List[object]
    try {
        [void]$nodes.Add(($Text | ConvertFrom-Json -Depth 100 -ErrorAction Stop))
    }
    catch {
        foreach ($line in [regex]::Split($Text, '\r?\n')) {
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            try {
                [void]$nodes.Add((ConvertFrom-Json -InputObject $line -Depth 100 -ErrorAction Stop))
            }
            catch {
                # Non-JSON residue already covered by substring check.
            }
        }
    }

    foreach ($node in $nodes) {
        Assert-JsonNodeScrubbed -Node $node -SourceName $SourceName
    }
}

function Assert-JsonNodeScrubbed {
    param($Node, [string]$SourceName)

    if ($null -eq $Node) { return }

    if ($Node -is [System.Array] -or ($Node -is [System.Collections.IEnumerable] -and $Node -isnot [string] -and $Node -isnot [System.Management.Automation.PSCustomObject])) {
        foreach ($item in @($Node)) {
            Assert-JsonNodeScrubbed -Node $item -SourceName $SourceName
        }
        return
    }

    if ($null -eq $Node.PSObject) { return }

    foreach ($prop in @($Node.PSObject.Properties)) {
        if ($null -eq $prop) { continue }
        $name = [string]$prop.Name
        $value = $prop.Value
        if (Test-IsHeaderContainerName -Name $name) {
            Assert-HeaderContainerScrubbed -Container $value -SourceName $SourceName
            continue
        }
        if (Test-IsCookieContainerName -Name $name) {
            foreach ($cookie in @($value)) {
                if ($null -eq $cookie) { continue }
                $cName = [string](Get-JsonPropertyIgnoreCase -Object $cookie -Name 'name')
                $cValue = [string](Get-JsonPropertyIgnoreCase -Object $cookie -Name 'value')
                if (-not [string]::IsNullOrEmpty($cValue) -and $cValue -ne (Get-PrAcceptanceRedactedValue)) {
                    throw "Scrub failed: cookie value for '$cName' not redacted in $SourceName"
                }
            }
            continue
        }
        Assert-JsonNodeScrubbed -Node $value -SourceName $SourceName
    }
}

function Assert-HeaderContainerScrubbed {
    param($Container, [string]$SourceName)
    if ($null -eq $Container) { return }

    if ($Container -is [System.Array] -or ($Container -is [System.Collections.IEnumerable] -and $Container -isnot [string])) {
        $list = @($Container)
        if ($list.Count -eq 0) { return }
        $first = $list[0]
        if ($first -is [System.Array] -or ($first -is [System.Collections.IList] -and $first -isnot [string])) {
            foreach ($pair in $list) {
                $items = @($pair)
                if ($items.Count -lt 2) { continue }
                $hName = [string]$items[0]
                $hValue = [string]$items[1]
                if ((Test-PrAcceptanceSecretHeaderName -Name $hName) -and $hValue -ne (Get-PrAcceptanceRedactedValue) -and -not [string]::IsNullOrEmpty($hValue)) {
                    throw "Scrub failed: denylisted header '$hName' value survives in $SourceName"
                }
            }
            return
        }
        if ($null -ne $first -and $null -ne (Get-JsonPropertyIgnoreCase -Object $first -Name 'name')) {
            foreach ($obj in $list) {
                $hName = [string](Get-JsonPropertyIgnoreCase -Object $obj -Name 'name')
                $hValue = [string](Get-JsonPropertyIgnoreCase -Object $obj -Name 'value')
                if ((Test-PrAcceptanceSecretHeaderName -Name $hName) -and $hValue -ne (Get-PrAcceptanceRedactedValue) -and -not [string]::IsNullOrEmpty($hValue)) {
                    throw "Scrub failed: denylisted header '$hName' value survives in $SourceName"
                }
            }
            return
        }
    }

    if ($null -ne $Container.PSObject) {
        foreach ($prop in @($Container.PSObject.Properties)) {
            $hName = [string]$prop.Name
            $hValue = if ($null -eq $prop.Value) { '' } else { [string]$prop.Value }
            if ((Test-PrAcceptanceSecretHeaderName -Name $hName) -and $hValue -ne (Get-PrAcceptanceRedactedValue) -and -not [string]::IsNullOrEmpty($hValue)) {
                throw "Scrub failed: denylisted header '$hName' value survives in $SourceName"
            }
        }
    }
}
