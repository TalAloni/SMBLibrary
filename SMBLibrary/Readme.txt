About SMBLibrary:
=================
SMBLibrary is an open-source C# SMB 1.0/CIFS 1.0 server implementation.
SMBLibrary gives .NET developers an easy way to share a directory / file system / virtual file system, with any operating system that supports the SMB protocol.
SMBLibrary shares can be accessed from any Windows version since Windows NT 4.0.

Supported SMB / CIFS transport methods:
=======================================
NetBIOS over TCP (port 139)
Direct TCP hosting (port 445)

Notes:
------
1. Windows bind port 139 on a per-adapter basis, while port 445 is bound globally.
This means that you can't use direct TCP hosting without disabling Windows File and Printer Sharing server completely.
However, NetBIOS over TCP is almost identical, and for this reason, it's recommended to use port 139.

2. To free port 139 for a given adapter, go to 'Internet Protocol (TCP/IP) Properties' > Advanced > WINS,
and select 'Disable NetBIOS over TCP/IP'. in addition you need to uncheck 'File and Printer Sharing for Microsoft Networks'.

3. It's important to note that disabling NetBIOS over TCP/IP will also disable NetBIOS name service for that adapter (a.k.a. WINS),
This service uses UDP port 137. SMBLibrary offers a name service of its own.

4. You can install a virtual network adapter driver for Windows to be used solely with SMBLibrary:
- You can install the 'Microsoft Loopback adapter' and use it for server-only communication with SMBLibrary.
- A limited alternative is 'OpenVPN TAP-Windows Adapter' that can be used for client communication with SMBLibrary,
However, you will have to configure this adapter to use a separate network segment.
The driver installation can be downloaded from: https://openvpn.net/index.php/open-source/downloads.html
To get started, go to Adapter properties > 'Advanced' and set 'Media Status' to 'Always Connected'.

5. The differences between 'Direct TCP hosting' and 'NetBIOS over TCP' are:
- A 'session request' packet is initiating the NBT connection.
- A 'keep alive' packet is sent from time to time over NBT connections.

Using SMBLibrary:
=================
Any directory / filesystem / object you wish to share must implement the IFileSystem interface.
You can share anything from actual directories to custom objects, as long as they expose a directory structure.

Contact:
========
If you have any question, feel free to contact me.
Tal Aloni <tal.aloni.il@gmail.com>