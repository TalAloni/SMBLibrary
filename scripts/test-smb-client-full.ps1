# SMBLibrary Comprehensive Client Test Script
# Usage: .\test-smb-client-full.ps1 -Servers @(@{IP="10.0.0.20";Name="FS1"},@{IP="10.0.0.21";Name="FS2"}) -Domain "LAB" -Username "Administrator" -Password "Password123!" -ShareName "Sales"

param(
    [Parameter(Mandatory=$false)]
    [array]$Servers = @(@{IP="127.0.0.1";Name="localhost"}),
    
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

Write-Host '=== SMBLibrary Comprehensive Client Tests ===' -ForegroundColor Cyan

function Test-SMBServer {
    param($ServerIP, $ServerName, $Domain, $Username, $Password, $ShareName)
    
    Write-Host "`n--- Testing $ServerName ($ServerIP) ---" -ForegroundColor Yellow
    
    $client = New-Object SMBLibrary.Client.SMB2Client
    $connected = $client.Connect([System.Net.IPAddress]::Parse($ServerIP), [SMBLibrary.SMBTransportType]::DirectTCPTransport)
    
    if (-not $connected) {
        Write-Host "  FAILED: Could not connect" -ForegroundColor Red
        return $false
    }
    Write-Host "  Connect: PASSED" -ForegroundColor Green
    
    $status = $client.Login($Domain, $Username, $Password)
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
    $fileStore = $client.TreeConnect($ShareName, $treeStatus)
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

# Test all servers
$results = @{}
foreach ($server in $Servers) {
    $results[$server.Name] = Test-SMBServer -ServerIP $server.IP -ServerName $server.Name -Domain $Domain -Username $Username -Password $Password -ShareName $ShareName
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
foreach ($name in $results.Keys) {
    $passed = $results[$name]
    Write-Host "$name`: $(if($passed){'PASSED'}else{'FAILED'})" -ForegroundColor $(if($passed){'Green'}else{'Red'})
}
