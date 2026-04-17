About SMBLibrary:
=================
SMBLibrary is an open-source C# SMB 1.0/CIFS, SMB 2.0, SMB 2.1 and SMB 3.0 server and client implementation.  
SMBLibrary gives .NET developers an easy way to share a directory / file system / virtual file system or to connect to an existing share, with any operating system that supports the SMB protocol.  
SMBLibrary is modular, you can take advantage of Integrated Windows Authentication and the Windows storage subsystem on a Windows host or use independent implementations that allow for cross-platform compatibility.  
SMBLibrary can communicate with any Windows version since Windows NT 4.0.  

Supported SMB / CIFS transport methods:
=======================================
• NetBIOS over TCP (port 139)  
• Direct TCP hosting (port 445)

###### 'NetBIOS over TCP' and 'Direct TCP hosting' are almost identical, the only differences:
- A 'session request' packet is initiating the NBT connection.
- A 'keep alive' packet is sent from time to time over NBT connections.
- SMB2: Direct TCP hosting supports large MTUs.

Using SMBLibrary:
=================
Server notes can be found [here](../ServerNotes.md).

Client code examples can be found [here](../ClientExamples.md).

NuGet Packages:
===============
[SMBLibrary](https://www.nuget.org/packages/SMBLibrary/) - Cross-platform server and client implementation.  
[SMBLibrary.Win32](https://www.nuget.org/packages/SMBLibrary.Win32/) - Allows utilizing Integrated Windows Authentication and/or the Windows storage subsystem on a Windows host.  
[SMBLibrary.Adapters](https://www.nuget.org/packages/SMBLibrary.Adapters/) - IFileSystem to INTFileStore adapter for SMBLibrary.  

Licensing:
==========
A commercial license of SMBLibrary is available for a fee.  
This is intended for companies who are unable to use the LGPL version.  
Please contact me for additional details.  

Contributions:
==============
If you choose to make a contribution to this project, you must agree to irrevocably assign to SMBLibrary and/or Tal Aloni all worldwide copyright and intellectual property rights in and to your contribution, effective upon submission.  

Contact:
========
If you have any question, feel free to contact me.  
Tal Aloni <tal.aloni.il@gmail.com>
