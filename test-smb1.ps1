Add-Type -Path 'c:\dev\SMBLibrary\SMBLibrary\bin\Debug\net40\SMBLibrary.dll'

Write-Host '=== SMB1 Client Test ===' -ForegroundColor Cyan

$client = New-Object SMBLibrary.Client.SMB1Client
Write-Host 'Connecting to LAB-FS1 via SMB1...'
$connected = $client.Connect([System.Net.IPAddress]::Parse('10.0.0.20'), [SMBLibrary.SMBTransportType]::DirectTCPTransport)
Write-Host "Connected: $connected"

if ($connected) {
    $status = $client.Login('LAB', 'Administrator', 'Password123!')
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
        $fileStore = $client.TreeConnect('Sales', $treeStatus)
        Write-Host "TreeConnect status: $($treeStatus.Value)"
        
        if ($fileStore) {
            Write-Host 'SMB1 TreeConnect: PASSED' -ForegroundColor Green
            $fileStore.Disconnect() | Out-Null
        }
        
        $client.Logoff() | Out-Null
    }
    $client.Disconnect()
} else {
    Write-Host 'SMB1 might be disabled on Windows Server 2022' -ForegroundColor Yellow
}
