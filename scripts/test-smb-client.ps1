# SMBLibrary Client Test Script
# Usage: .\test-smb-client.ps1 -ServerIP "10.0.0.20" -Domain "LAB" -Username "Administrator" -Password "Password123!" -ShareName "Sales"

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

Write-Host "=== SMBLibrary Client Test ===" -ForegroundColor Cyan

# Test 1: Connect
Write-Host "`n[Test 1] Connect to $ServerIP..."
$client = New-Object SMBLibrary.Client.SMB2Client
$connected = $client.Connect([System.Net.IPAddress]::Parse($ServerIP), [SMBLibrary.SMBTransportType]::DirectTCPTransport)
Write-Host "Connected: $connected"

if ($connected) {
    # Test 2: Login
    Write-Host "`n[Test 2] Login as $Domain\$Username..."
    $status = $client.Login($Domain, $Username, $Password)
    Write-Host "Login status: $status"
    
    if ($status -eq [SMBLibrary.NTStatus]::STATUS_SUCCESS) {
        # Test 3: List shares
        Write-Host "`n[Test 3] List shares..."
        $listStatus = [ref][SMBLibrary.NTStatus]::STATUS_SUCCESS
        $shares = $client.ListShares($listStatus)
        Write-Host "ListShares status: $($listStatus.Value)"
        if ($shares) {
            $shares | ForEach-Object { Write-Host "  - $_" }
        }
        
        # Test 4: TreeConnect
        Write-Host "`n[Test 4] TreeConnect to $ShareName share..."
        $treeStatus = [ref][SMBLibrary.NTStatus]::STATUS_SUCCESS
        $fileStore = $client.TreeConnect($ShareName, $treeStatus)
        Write-Host "TreeConnect status: $($treeStatus.Value)"
        
        if ($fileStore) {
            # Test 5: List files in root
            Write-Host "`n[Test 5] Query directory..."
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

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
