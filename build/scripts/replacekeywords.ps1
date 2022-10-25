param(
    [parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [System.IO.FileInfo]$InPath,
 
    [parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [System.IO.FileInfo]$OutPath,

    [parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$srcwords,

    [parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$destwords
)
Process {
    try {
		gc $InPath.FullName | foreach { $_ -replace $srcwords, $destwords} | sc $OutPath.FullName
    } 
    catch {
        Write-Warning -Message $_.Exception.Message ; break
    }
}
End {
    # Run garbage collection and release ComObject
    [System.GC]::Collect()
}
