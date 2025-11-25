Add-Type -Path 'c:\dev\SMBLibrary\SMBLibrary\bin\Debug\net40\SMBLibrary.dll'

Write-Host '=== SMBLibrary Client Test against Lab ===' -ForegroundColor Cyan

# Test 1: Connect to FS1 via IP
Write-Host '
[Test 1] Connect to LAB-FS1 (10.0.0.20)...'
$client = New-Object SMBLibrary.Client.SMB2Client
$connected = $client.Connect([System.Net.IPAddress]::Parse('10.0.0.20'), [SMBLibrary.SMBTransportType]::DirectTCPTransport)
Write-Host "Connected: $connected"

if ($connected) {
    # Test 2: Login
    Write-Host '
[Test 2] Login as LAB\Administrator...'
    $status = $client.Login('LAB', 'Administrator', 'Password123!')
    Write-Host "Login status: $status"
    
    if ($status -eq [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
        # Test 3: List shares
        Write-Host '
[Test 3] List shares...'
        $listStatus = [ref][SMBLibrary.NTStatus]::STATUS_SUCCESS
        $shares = $client.ListShares($listStatus)
        Write-Host "ListShares status: $($listStatus.Value)"
        if ($shares) {
            $shares | ForEach-Object { Write-Host "  - $_" }
        }
        
        # Test 4: TreeConnect to Sales share
        Write-Host '
[Test 4] TreeConnect to Sales share...'
        $treeStatus = [ref][SMBLibrary.NTStatus]::STATUS_SUCCESS
        $fileStore = $client.TreeConnect('Sales', $treeStatus)
        Write-Host "TreeConnect status: $($treeStatus.Value)"
        
        if ($fileStore) {
            # Test 5: List files in root
            Write-Host '
[Test 5] Query directory...'
            $handle = [ref]$null
            $fileStatus = [ref][SMBLibrary.FileStatus]::FILE_DOES_NOT_EXIST
            $createStatus = $fileStore.CreateFile($handle, $fileStatus, '', 
                [SMBLibrary.AccessMask]::GENERIC_READ, 
                [SMBLibrary.FileAttributes]::Directory,
                [SMBLibrary.ShareAccess]::Read,
                [SMBLibrary.CreateDisposition]::FILE_OPEN,
                [SMBLibrary.CreateOptions]::FILE_DIRECTORY_FILE,
                $null)
            Write-Host "CreateFile (dir) status: $createStatus"
            
            if ($createStatus -eq [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
                $entries = [ref]$null
                $queryStatus = $fileStore.QueryDirectory($entries, $handle.Value, '*', [SMBLibrary.FileInformationClass]::FileDirectoryInformation)
                Write-Host "QueryDirectory status: $queryStatus"
                if ($entries.Value) {
                    $entries.Value | ForEach-Object { Write-Host "  - $($_.FileName)" }
                }
                $fileStore.CloseFile($handle.Value) | Out-Null
            }
            
            $fileStore.Disconnect() | Out-Null
        }
        
        $client.Logoff() | Out-Null
    }
    $client.Disconnect()
}

Write-Host '
=== Test Complete ===' -ForegroundColor Cyan
