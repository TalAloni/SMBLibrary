# SMBLibrary SMB1 Client Test Script
# Usage: .\test-smb1.ps1 -ServerIP "10.0.0.20" -Domain "LAB" -Username "Administrator" -Password "Password123!" -ShareName "Sales"

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerIP,
    
    [Parameter(Mandatory=$false)]
    [string]$Domain = "",
    
    [Parameter(Mandatory=$true)]
    [string]$Username,
    
    [Parameter(Mandatory=$true)]
    [string]$Password,
    
    [Parameter(Mandatory=$false)]
    [string]$ShareName = "Shared",
    
    [Parameter(Mandatory=$false)]
    [string]$DllPath = "$PSScriptRoot\..\SMBLibrary\bin\Debug\net40\SMBLibrary.dll"
)

Add-Type -Path $DllPath

Write-Host '=== SMB1 Client Test ===' -ForegroundColor Cyan

$client = New-Object SMBLibrary.Client.SMB1Client
Write-Host "Connecting to $ServerIP via SMB1..."
$connected = $client.Connect([System.Net.IPAddress]::Parse($ServerIP), [SMBLibrary.SMBTransportType]::DirectTCPTransport)
Write-Host "Connected: $connected"

if ($connected) {
    $status = $client.Login($Domain, $Username, $Password)
    Write-Host "Login status: $status"
    
    if ($status -eq [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
        $listStatus = [ref][SMBLibrary.NTStatus]::STATUS_SUCCESS
        $shares = $client.ListShares($listStatus)
        Write-Host "ListShares status: $($listStatus.Value)"
        if ($shares) {
            $shares | ForEach-Object { Write-Host "  - $_" }
        }
        
        # TreeConnect
        $treeStatus = [ref][SMBLibrary.NTStatus]::STATUS_SUCCESS
        $fileStore = $client.TreeConnect($ShareName, $treeStatus)
        Write-Host "TreeConnect status: $($treeStatus.Value)"
        
        if ($fileStore) {
            Write-Host 'SMB1 TreeConnect: PASSED' -ForegroundColor Green
            $fileStore.Disconnect() | Out-Null
        }
        
        $client.Logoff() | Out-Null
    }
    $client.Disconnect()
} else {
    Write-Host 'SMB1 might be disabled on the target server' -ForegroundColor Yellow
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
