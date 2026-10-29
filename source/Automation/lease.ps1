param([Parameter(Mandatory=$true)][string]$Name)
$ErrorActionPreference='Stop'
$leaseMutex=[Threading.Mutex]::new($false,$Name)
$leaseTaken=$false
try {
    try {$leaseTaken=$leaseMutex.WaitOne(0)}catch [Threading.AbandonedMutexException]{$leaseTaken=$true}
    if(-not $leaseTaken){exit 3}
    [Console]::WriteLine('ACQUIRED');[Console]::Out.Flush()
    # EOF also releases the lease if the owning Node process exits unexpectedly.
    [void][Console]::ReadLine()
} finally {if($leaseTaken){$leaseMutex.ReleaseMutex()};$leaseMutex.Dispose()}
