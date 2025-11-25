Add-Type -Path 'c:\dev\SMBLibrary\SMBLibrary\bin\Debug\net40\SMBLibrary.dll'

Write-Host '=== SMBLibrary Comprehensive Client Tests ===' -ForegroundColor Cyan

function Test-SMBServer {
    param($ServerIP, $ServerName)
    
    Write-Host "
--- Testing $ServerName ($ServerIP) ---" -ForegroundColor Yellow
    
    $client = New-Object SMBLibrary.Client.SMB2Client
    $connected = $client.Connect([System.Net.IPAddress]::Parse($ServerIP), [SMBLibrary.SMBTransportType]::DirectTCPTransport)
    
    if (-not $connected) {
        Write-Host "  FAILED: Could not connect" -ForegroundColor Red
        return $false
    }
    Write-Host "  Connect: PASSED" -ForegroundColor Green
    
    $status = $client.Login('LAB', 'Administrator', 'Password123!')
    if ($status -ne [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
        Write-Host "  FAILED: Login returned $status" -ForegroundColor Red
        $client.Disconnect()
        return $false
    }
    Write-Host "  Login: PASSED" -ForegroundColor Green
    
    # List shares
    $listStatus = [ref][SMBLibrary.NTStatus]::STATUS_SUCCESS
    $shares = $client.ListShares($listStatus)
    if ($listStatus.Value -ne [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
        Write-Host "  FAILED: ListShares returned $($listStatus.Value)" -ForegroundColor Red
    } else {
        Write-Host "  ListShares: PASSED ($($shares.Count) shares)" -ForegroundColor Green
    }
    
    # TreeConnect
    $treeStatus = [ref][SMBLibrary.NTStatus]::STATUS_SUCCESS
    $fileStore = $client.TreeConnect('Sales', $treeStatus)
    if ($treeStatus.Value -ne [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
        Write-Host "  FAILED: TreeConnect returned $($treeStatus.Value)" -ForegroundColor Red
        $client.Logoff() | Out-Null
        $client.Disconnect()
        return $false
    }
    Write-Host "  TreeConnect: PASSED" -ForegroundColor Green
    
    # Write test file
    $testFileName = "smblibrary-test-$(Get-Date -Format 'yyyyMMddHHmmss').txt"
    $testContent = [System.Text.Encoding]::UTF8.GetBytes("SMBLibrary test from $ServerName at $(Get-Date)")
    
    $handle = [ref]$null
    $fileStatus = [ref][SMBLibrary.FileStatus]::FILE_DOES_NOT_EXIST
    $createStatus = $fileStore.CreateFile($handle, $fileStatus, $testFileName, 
        [SMBLibrary.AccessMask]::GENERIC_WRITE -bor [SMBLibrary.AccessMask]::GENERIC_READ, 
        [SMBLibrary.FileAttributes]::Normal,
        [SMBLibrary.ShareAccess]::None,
        [SMBLibrary.CreateDisposition]::FILE_CREATE,
        [SMBLibrary.CreateOptions]::FILE_NON_DIRECTORY_FILE,
        $null)
    
    if ($createStatus -ne [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
        Write-Host "  FAILED: CreateFile (write) returned $createStatus" -ForegroundColor Red
    } else {
        $bytesWritten = [ref]0
        $writeStatus = $fileStore.WriteFile($bytesWritten, $handle.Value, 0, $testContent)
        $fileStore.CloseFile($handle.Value) | Out-Null
        
        if ($writeStatus -ne [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
            Write-Host "  FAILED: WriteFile returned $writeStatus" -ForegroundColor Red
        } else {
            Write-Host "  WriteFile: PASSED ($($bytesWritten.Value) bytes)" -ForegroundColor Green
        }
    }
    
    # Read test file back
    $handle = [ref]$null
    $fileStatus = [ref][SMBLibrary.FileStatus]::FILE_DOES_NOT_EXIST
    $createStatus = $fileStore.CreateFile($handle, $fileStatus, $testFileName, 
        [SMBLibrary.AccessMask]::GENERIC_READ, 
        [SMBLibrary.FileAttributes]::Normal,
        [SMBLibrary.ShareAccess]::Read,
        [SMBLibrary.CreateDisposition]::FILE_OPEN,
        [SMBLibrary.CreateOptions]::FILE_NON_DIRECTORY_FILE,
        $null)
    
    if ($createStatus -ne [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
        Write-Host "  FAILED: CreateFile (read) returned $createStatus" -ForegroundColor Red
    } else {
        $data = [ref]$null
        $readStatus = $fileStore.ReadFile($data, $handle.Value, 0, 4096)
        $fileStore.CloseFile($handle.Value) | Out-Null
        
        if ($readStatus -ne [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
            Write-Host "  FAILED: ReadFile returned $readStatus" -ForegroundColor Red
        } else {
            $readText = [System.Text.Encoding]::UTF8.GetString($data.Value)
            Write-Host "  ReadFile: PASSED ($($data.Value.Length) bytes)" -ForegroundColor Green
        }
    }
    
    # Echo test
    $echoStatus = $client.Echo()
    if ($echoStatus -ne [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
        Write-Host "  FAILED: Echo returned $echoStatus" -ForegroundColor Red
    } else {
        Write-Host "  Echo: PASSED" -ForegroundColor Green
    }
    
    $fileStore.Disconnect() | Out-Null
    $client.Logoff() | Out-Null
    $client.Disconnect()
    
    return $true
}

# Test both file servers
$fs1Result = Test-SMBServer -ServerIP '10.0.0.20' -ServerName 'LAB-FS1'
$fs2Result = Test-SMBServer -ServerIP '10.0.0.21' -ServerName 'LAB-FS2'

Write-Host "
=== Summary ===" -ForegroundColor Cyan
Write-Host "LAB-FS1: $(if($fs1Result){'PASSED'}else{'FAILED'})" -ForegroundColor $(if($fs1Result){'Green'}else{'Red'})
Write-Host "LAB-FS2: $(if($fs2Result){'PASSED'}else{'FAILED'})" -ForegroundColor $(if($fs2Result){'Green'}else{'Red'})
