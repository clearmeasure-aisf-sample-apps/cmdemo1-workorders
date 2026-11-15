#!/usr/bin/env pwsh
#Requires -Version 7.4

<#
.SYNOPSIS
  Resolve the HTTPS base URL of a leased pool slot container app.

.DESCRIPTION
  Reads the Container Apps ingress FQDN for ca-<slug>-<slot> in the nonprod
  resource group. Writes application_base_url to GITHUB_OUTPUT when set.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Slot,

    [Parameter(Mandatory)]
    [string] $Slug,

    [Parameter(Mandatory)]
    [string] $ResourceGroup
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$appName = "ca-$Slug-$Slot"
$fqdn = az containerapp show -n $appName -g $ResourceGroup --query 'properties.configuration.ingress.fqdn' -o tsv
if ([string]::IsNullOrWhiteSpace($fqdn)) {
    throw "No ingress FQDN for container app $appName in $ResourceGroup"
}

$url = "https://$($fqdn.Trim())"
if ($env:GITHUB_OUTPUT) {
    "application_base_url=$url" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}
Write-Host "application_base_url=$url"
Write-Output $url
