Login and list shares:
======================
```cs
SMB1Client client = new SMB1Client(); // SMB2Client can be used as well
bool isConnected = client.Connect(IPAddress.Parse("192.168.1.11"), SMBTransportType.DirectTCPTransport);
if (isConnected)
{
    NTStatus status = client.Login(String.Empty, "Username", "Password");
    if (status == NTStatus.STATUS_SUCCESS)
    {
        List<string> shares = client.ListShares(out status);
        client.Logoff();
    }
    client.Disconnect();
}
```

Connect to share and list files and directories - SMB1:
=======================================================
```cs
ISMBFileStore fileStore = client.TreeConnect("Shared", out status);
if (status == NTStatus.STATUS_SUCCESS)
{
    object directoryHandle;
    FileStatus fileStatus;
    status = fileStore.CreateFile(out directoryHandle, out fileStatus, "\\", AccessMask.GENERIC_READ, FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
    if (status == NTStatus.STATUS_SUCCESS)
    {
        List<FindInformation> fileList2;
        status = ((SMB1FileStore)fileStore).QueryDirectory(out fileList2, "\\*", FindInformationLevel.SMB_FIND_FILE_DIRECTORY_INFO);
        status = fileStore.CloseFile(directoryHandle);
    }
}
status = fileStore.Disconnect();
```

DFS-enabled SMB2 client using DfsClientFactory:
==============================================
> **Note**: For detailed DFS configuration options and troubleshooting, see [docs/dfs-usage.md](docs/dfs-usage.md).
```cs
using System;
using System.Collections.Generic;
using System.Net;
using SMBLibrary;
using SMBLibrary.Client;
using SMBLibrary.Client.DFS;

// Connect and login (DFS is still disabled by default)
SMB2Client client = new SMB2Client();
bool isConnected = client.Connect(IPAddress.Parse("192.168.1.11"), SMBTransportType.DirectTCPTransport);
if (!isConnected)
{
    throw new Exception("Failed to connect to server");
}

NTStatus status = client.Login(String.Empty, "Username", "Password");
if (status != NTStatus.STATUS_SUCCESS)
{
    client.Disconnect();
    throw new Exception("Failed to login");
}

// Connect to a share as usual
ISMBFileStore baseStore = client.TreeConnect("Public", out status);
if (status != NTStatus.STATUS_SUCCESS || baseStore == null)
{
    client.Logoff();
    client.Disconnect();
    throw new Exception("Failed to tree connect");
}

// Enable DFS behavior via DfsClientOptions and DfsClientFactory
DfsClientOptions dfsOptions = new DfsClientOptions();
dfsOptions.Enabled = true; // DFS remains opt-in

// dfsHandle can be null for default SMB2 DFS IOCTL behavior over the share
ISMBFileStore dfsStore = DfsClientFactory.CreateDfsAwareFileStore(baseStore, null, dfsOptions);

// Use dfsStore like any other ISMBFileStore; DFS-aware behavior is internal
object directoryHandle;
FileStatus fileStatus;
status = dfsStore.CreateFile(out directoryHandle, out fileStatus, "\\\\contoso.com\\Public", AccessMask.GENERIC_READ, FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
if (status == NTStatus.STATUS_SUCCESS)
{
    List<QueryDirectoryFileInformation> fileList;
    status = dfsStore.QueryDirectory(out fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
    status = dfsStore.CloseFile(directoryHandle);
}

client.Logoff();
client.Disconnect();
```

Connect to share and list files and directories - SMB2:
=======================================================
```cs
ISMBFileStore fileStore = client.TreeConnect("Shared", out status);
if (status == NTStatus.STATUS_SUCCESS)
{
    object directoryHandle;
    FileStatus fileStatus;
    status = fileStore.CreateFile(out directoryHandle, out fileStatus, String.Empty, AccessMask.GENERIC_READ, FileAttributes.Directory, ShareAccess.Read | ShareAccess.Write, CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);
    if (status == NTStatus.STATUS_SUCCESS)
    {
        List<QueryDirectoryFileInformation> fileList;
        status = fileStore.QueryDirectory(out fileList, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
        status = fileStore.CloseFile(directoryHandle);
    }
}
status = fileStore.Disconnect();
```

Read large file to its end:
===========================
```cs
ISMBFileStore fileStore = client.TreeConnect("Shared", out status);
object fileHandle;
FileStatus fileStatus;
string filePath = "IMG_20190109_174446.jpg";
if (fileStore is SMB1FileStore)
{
    filePath = @"\\" + filePath;
}
status = fileStore.CreateFile(out fileHandle, out fileStatus, filePath, AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.Read, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

if (status == NTStatus.STATUS_SUCCESS)
{
    System.IO.MemoryStream stream = new System.IO.MemoryStream();
    byte[] data;
    long bytesRead = 0;
    while (true)
    {
        status = fileStore.ReadFile(out data, fileHandle, bytesRead, (int)client.MaxReadSize);
        if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
        {
            throw new Exception("Failed to read from file");
        }

        if (status == NTStatus.STATUS_END_OF_FILE || data.Length == 0)
        {
            break;
        }
        bytesRead += data.Length;
        stream.Write(data, 0, data.Length);
    }
}
status = fileStore.CloseFile(fileHandle);
status = fileStore.Disconnect();
```

Create a file and write to it:
==============================
```cs
ISMBFileStore fileStore = client.TreeConnect("Shared", out status);
string filePath = "NewFile.txt";
if (fileStore is SMB1FileStore)
{
    filePath = @"\\" + filePath;
}
object fileHandle;
FileStatus fileStatus;
status = fileStore.CreateFile(out fileHandle, out fileStatus, filePath, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
if (status == NTStatus.STATUS_SUCCESS)
{
    int numberOfBytesWritten;
    byte[] data = System.Text.ASCIIEncoding.ASCII.GetBytes("Hello");
    status = fileStore.WriteFile(out numberOfBytesWritten, fileHandle, 0, data);
    if (status != NTStatus.STATUS_SUCCESS)
    {
        throw new Exception("Failed to write to file");
    }
    status = fileStore.CloseFile(fileHandle);
}
status = fileStore.Disconnect();
```

Write a large file:
===================
```cs
ISMBFileStore fileStore = client.TreeConnect("Shared", out status);
if (status != NTStatus.STATUS_SUCCESS)
{
    throw new Exception("Failed to connect to share");
}
string localFilePath = @"C:\Image.jpg";
string remoteFilePath = "NewFile.jpg";
if (fileStore is SMB1FileStore)
{
    remoteFilePath = @"\\" + remoteFilePath;
}
FileStream localFileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
object fileHandle;
FileStatus fileStatus;
status = fileStore.CreateFile(out fileHandle, out fileStatus, remoteFilePath, AccessMask.GENERIC_WRITE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_CREATE, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);
if (status == NTStatus.STATUS_SUCCESS)
{
    long writeOffset = 0;
    while (localFileStream.Position < localFileStream.Length)
    {
        byte[] buffer = new byte[(int)client.MaxWriteSize];
        int bytesRead = localFileStream.Read(buffer, 0, buffer.Length);
        if (bytesRead < (int)client.MaxWriteSize)
        {
            Array.Resize<byte>(ref buffer, bytesRead);
        }
        int numberOfBytesWritten;
        status = fileStore.WriteFile(out numberOfBytesWritten, fileHandle, writeOffset, buffer);
        if (status != NTStatus.STATUS_SUCCESS)
        {
            throw new Exception("Failed to write to file");
        }
        writeOffset += bytesRead;
    }
    status = fileStore.CloseFile(fileHandle);
}
status = fileStore.Disconnect();
```

Delete file:
============
```cs
ISMBFileStore fileStore = client.TreeConnect("Shared", out status);
string filePath = "DeleteMe.txt";
if (fileStore is SMB1FileStore)
{
    filePath = @"\\" + filePath;
}
object fileHandle;
FileStatus fileStatus;
status = fileStore.CreateFile(out fileHandle, out fileStatus, filePath, AccessMask.GENERIC_WRITE | AccessMask.DELETE | AccessMask.SYNCHRONIZE, FileAttributes.Normal, ShareAccess.None, CreateDisposition.FILE_OPEN, CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT, null);

if (status == NTStatus.STATUS_SUCCESS)
{
    FileDispositionInformation fileDispositionInformation = new FileDispositionInformation();
    fileDispositionInformation.DeletePending = true;
    status = fileStore.SetFileInformation(fileHandle, fileDispositionInformation);
    bool deleteSucceeded = (status == NTStatus.STATUS_SUCCESS);
    status = fileStore.CloseFile(fileHandle);
}
status = fileStore.Disconnect();
```
