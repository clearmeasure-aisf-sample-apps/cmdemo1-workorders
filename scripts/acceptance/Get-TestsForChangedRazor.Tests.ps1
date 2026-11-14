#Requires -Version 7.0
# Self-tests for Get-TestsForChangedRazor.ps1. No network. Invoked the CI way:
#   pwsh -NoProfile -File scripts/acceptance/Get-TestsForChangedRazor.Tests.ps1
# or:
#   pwsh -NoProfile -File scripts/acceptance/Get-TestsForChangedRazor.ps1 -SelfTest
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Get-TestsForChangedRazor.ps1'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Invoke-HelperFile {
    param([string[]]$ArgumentList = @(), [string]$Stdin = $null)
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = 'pwsh'
    $psi.ArgumentList.Add('-NoProfile')
    $psi.ArgumentList.Add('-File')
    $psi.ArgumentList.Add($scriptPath)
    foreach ($arg in $ArgumentList) { $psi.ArgumentList.Add($arg) }
    $psi.WorkingDirectory = $repoRoot
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $proc = [System.Diagnostics.Process]::Start($psi)
    if ($null -eq $proc) { throw 'Failed to start pwsh -File helper.' }
    if ($null -ne $Stdin) {
        $proc.StandardInput.Write($Stdin)
    }
    $proc.StandardInput.Close()
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()
    return [pscustomobject]@{
        ExitCode = $proc.ExitCode
        StdOut   = $stdout
        StdErr   = $stderr
    }
}

function Invoke-HelperCommand {
    param([string]$InnerCommand = '', [string]$Stdin = '')
    $wrapped = if ([string]::IsNullOrWhiteSpace($InnerCommand)) {
        "& '$scriptPath'"
    }
    else {
        "& '$scriptPath' $InnerCommand"
    }
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = 'pwsh'
    $psi.ArgumentList.Add('-NoProfile')
    $psi.ArgumentList.Add('-Command')
    $psi.ArgumentList.Add($wrapped)
    $psi.WorkingDirectory = $repoRoot
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $proc = [System.Diagnostics.Process]::Start($psi)
    if ($null -eq $proc) { throw 'Failed to start pwsh -Command helper.' }
    $proc.StandardInput.Write($Stdin)
    $proc.StandardInput.Close()
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()
    return [pscustomobject]@{
        ExitCode = $proc.ExitCode
        StdOut   = $stdout
        StdErr   = $stderr
    }
}

function Get-OutputLines {
    param([string]$Text)
    return @(
        $Text -split "`r?`n" |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_ -ne '' }
    )
}

function Test-RazorPathsSelectsLogin {
    $result = Invoke-HelperFile -ArgumentList @(
        '-RazorPaths', 'src/UI.Shared/Pages/Login.razor'
    )
    Assert-True ($result.ExitCode -eq 0) "RazorPaths Login exit $($result.ExitCode): $($result.StdErr)"
    $lines = Get-OutputLines $result.StdOut
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests') `
        'RazorPaths Login must select LoginTests.'
    Write-Output 'RazorPaths Login ok'
}

function Test-PathsFileSelectsLogin {
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) ("razor-paths-" + [guid]::NewGuid().ToString('N') + '.txt')
    try {
        Set-Content -LiteralPath $temp -Value "src/UI.Shared/Pages/Login.razor`n" -Encoding utf8NoBOM
        $result = Invoke-HelperFile -ArgumentList @('-PathsFile', $temp)
        Assert-True ($result.ExitCode -eq 0) "PathsFile Login exit $($result.ExitCode): $($result.StdErr)"
        $lines = Get-OutputLines $result.StdOut
        Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests') `
            'PathsFile Login must select LoginTests.'
        Write-Output 'PathsFile Login ok'
    }
    finally {
        Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue
    }
}

function Test-StdinViaFileSelectsLogin {
    $result = Invoke-HelperFile -Stdin "src/UI.Shared/Pages/Login.razor`n"
    Assert-True ($result.ExitCode -eq 0) "stdin -File exit $($result.ExitCode): $($result.StdErr)"
    $lines = Get-OutputLines $result.StdOut
    Assert-True ($lines.Count -gt 0) 'stdin -File must print fixture names (not a false empty).'
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests') `
        'stdin -File must select LoginTests.'
    Assert-True ($result.StdErr -notmatch 'cannot be bound to any parameters') `
        'stdin -File must not emit a parameter-binding error.'
    Write-Output 'stdin via -File ok'
}

function Test-StdinViaCommandSelectsLogin {
    $result = Invoke-HelperCommand -Stdin "src/UI.Shared/Pages/Login.razor`n"
    Assert-True ($result.ExitCode -eq 0) "stdin -Command exit $($result.ExitCode): $($result.StdErr)"
    $lines = Get-OutputLines $result.StdOut
    Assert-True ($lines.Count -gt 0) 'stdin -Command must print fixture names (not a false empty).'
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests') `
        'stdin -Command must select LoginTests.'
    Write-Output 'stdin via -Command ok'
}

function Test-PathsFileDashFailsLoudly {
    $result = Invoke-HelperFile -ArgumentList @('-PathsFile', '-') -Stdin "src/UI.Shared/Pages/Login.razor`n"
    Assert-True ($result.ExitCode -ne 0) 'PathsFile - must exit non-zero.'
    Assert-True (($result.StdErr + $result.StdOut) -match "PathsFile '-' is not supported") `
        'PathsFile - must name the unsupported form.'
    Write-Output 'PathsFile - fails loudly ok'
}

function Test-BackslashPathSeparator {
    $result = Invoke-HelperFile -ArgumentList @(
        '-RazorPaths', 'src\UI.Shared\Pages\Login.razor'
    )
    Assert-True ($result.ExitCode -eq 0) "backslash exit $($result.ExitCode): $($result.StdErr)"
    $lines = Get-OutputLines $result.StdOut
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests') `
        'Backslash Razor path must match the forward-slash map entry.'
    Write-Output 'backslash path separator ok'
}

function Test-SharedComponentTeamCards {
    $result = Invoke-HelperFile -ArgumentList @(
        '-RazorPaths', 'src/UI.Shared/Components/Teams/TeamCards.razor'
    )
    Assert-True ($result.ExitCode -eq 0) "TeamCards exit $($result.ExitCode): $($result.StdErr)"
    $lines = Get-OutputLines $result.StdOut
    Assert-True ($lines.Count -gt 0) 'TeamCards must not look like no tests.'
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.TeamsAndVendors.TeamsPageTests') `
        'TeamCards must select TeamsPageTests.'
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.TeamsAndVendors.VendorsPageTests') `
        'TeamCards must select VendorsPageTests.'
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.App.AccessibilityAxeTeamsTests') `
        'TeamCards must select AccessibilityAxeTeamsTests.'
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.App.PhoneWidthNoDocumentScrollTests') `
        'TeamCards must select PhoneWidthNoDocumentScrollTests.'
    Write-Output 'shared component TeamCards ok'
}

function Test-PageHeaderHostsArePagesThatRenderIt {
    $result = Invoke-HelperFile -ArgumentList @(
        '-RazorPaths', 'src/UI.Shared/Components/PageHeader.razor'
    )
    Assert-True ($result.ExitCode -eq 0) "PageHeader exit $($result.ExitCode): $($result.StdErr)"
    $lines = Get-OutputLines $result.StdOut
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.App.ReportsPageTests') `
        'PageHeader must select ReportsPageTests (Reports renders PageHeader).'
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.App.LandingPageTests') `
        'PageHeader must select LandingPageTests (Index renders PageHeader).'
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSearchTests') `
        'PageHeader must select WorkOrderSearchTests (Search renders PageHeader).'
    Write-Output 'PageHeader host pages ok'
}

function Test-LastWorkOrderHostsNavMenu {
    $result = Invoke-HelperFile -ArgumentList @(
        '-RazorPaths', 'src/UI.Shared/Components/LastWorkOrder.razor'
    )
    Assert-True ($result.ExitCode -eq 0) "LastWorkOrder exit $($result.ExitCode): $($result.StdErr)"
    $lines = Get-OutputLines $result.StdOut
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.App.NavMenuTests') `
        'LastWorkOrder must select NavMenuTests (NavMenu renders LastWorkOrder).'
    Write-Output 'LastWorkOrder NavMenu host ok'
}

function Test-AssigneeAvatarRecursesToSearch {
    $result = Invoke-HelperFile -ArgumentList @(
        '-RazorPaths', 'src/UI.Shared/Components/Search/AssigneeAvatar.razor'
    )
    Assert-True ($result.ExitCode -eq 0) "AssigneeAvatar exit $($result.ExitCode): $($result.StdErr)"
    $lines = Get-OutputLines $result.StdOut
    Assert-True ($lines -contains 'ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders.WorkOrderSearchTests') `
        'AssigneeAvatar must recurse Board/Table hosts to WorkOrderSearch fixtures.'
    Write-Output 'AssigneeAvatar recursive hosts ok'
}

function Test-UnmappedUiRazorFailsLoudly {
    $result = Invoke-HelperFile -ArgumentList @(
        '-RazorPaths', 'src/UI.Shared/Pages/DoesNotExist.razor'
    )
    Assert-True ($result.ExitCode -ne 0) 'An unmapped UI Razor path must exit non-zero.'
    Assert-True (($result.StdErr + $result.StdOut) -match 'No acceptance fixtures mapped') `
        'Unmapped UI Razor must report that no fixtures mapped.'
    Write-Output 'unmapped UI Razor fails loudly ok'
}

function Test-EmptyStdinExitsZeroWithNoNames {
    $result = Invoke-HelperFile -Stdin ''
    Assert-True ($result.ExitCode -eq 0) "empty stdin exit $($result.ExitCode): $($result.StdErr)"
    $lines = Get-OutputLines $result.StdOut
    Assert-True ($lines.Count -eq 0) 'Empty stdin (no changed Razor) must print no names.'
    Write-Output 'empty stdin ok'
}

Test-RazorPathsSelectsLogin
Test-PathsFileSelectsLogin
Test-StdinViaFileSelectsLogin
Test-StdinViaCommandSelectsLogin
Test-PathsFileDashFailsLoudly
Test-BackslashPathSeparator
Test-SharedComponentTeamCards
Test-PageHeaderHostsArePagesThatRenderIt
Test-LastWorkOrderHostsNavMenu
Test-AssigneeAvatarRecursesToSearch
Test-UnmappedUiRazorFailsLoudly
Test-EmptyStdinExitsZeroWithNoNames
Write-Output 'Get-TestsForChangedRazor.Tests.ps1 passed'
