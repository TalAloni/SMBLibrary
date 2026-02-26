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

Cross-platform Kerberos authentication:
=======================================
You can have cross-platform Kerberos login support by creating a class that implements IAuthenticationClient.  
[Kerberos.NET](https://github.com/dotnet/Kerberos.NET) can easily be used to implement IAuthenticationClient.  
Note that in order for Kerberos.NET to work on non-Windows platforms, you must provide a cross-platform implementation of IKerberosDnsQuery (and register it using DnsQuery.RegisterImplementation)  
[DnsClient.NET](https://github.com/MichaCo/DnsClient.NET) can easily be used to implement IKerberosDnsQuery.  

```cs
public class KerberosNetAuthenticationClient : IAuthenticationClient
{
    private readonly KerberosClient m_kerberosClient;
    private readonly string m_spn;
    private byte[] m_sessionKey;

    public KerberosNetAuthenticationClient(string user, string password, string domain, string host)
    {
        m_kerberosClient = new KerberosClient();
        m_kerberosClient.Authenticate(new KerberosPasswordCredential(user, password, domain)).Wait();
        m_spn = $"cifs/{host}";
    }

    public byte[] InitializeSecurityContext(byte[] inputToken)
    {
        KrbApReq ticket = m_kerberosClient.GetServiceTicket(m_spn).GetAwaiter().GetResult();
        KerberosClientCacheEntry cachedItem = (KerberosClientCacheEntry)m_kerberosClient.Cache.GetCacheItem(m_spn);
        m_sessionKey = cachedItem.SessionKey.KeyValue.ToArray();
        return ticket.EncodeGssApi().ToArray();
    }

    public byte[] GetSessionKey() => m_sessionKey;
}
```