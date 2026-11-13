<#
.SYNOPSIS
  Prints the NUnit filter for the serial acceptance lane.

.DESCRIPTION
  Serial-lane tests are tagged Category(SerialLane). They flip process-wide state
  (or are not yet independent) and must run after every parallel shard on the same
  pool slot finishes — never in a shard that runs beside the parallel lane.

  Pair with Write-AcceptanceRunSettings.ps1 -Workers 1 for the serial invocation.
#>
[CmdletBinding()]
param()

Write-Output "TestCategory==SerialLane"
