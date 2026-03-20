About SMBLibrary:
=================
SMBLibrary is an open-source C# SMB 1.0/CIFS, SMB 2.0, SMB 2.1 and SMB 3.0 server and client implementation.  
SMBLibrary gives .NET developers an easy way to share a directory / file system / virtual file system, with any operating system that supports the SMB protocol.  
SMBLibrary is modular, you can take advantage of Integrated Windows Authentication and the Windows storage subsystem on a Windows host or use independent implementations that allow for cross-platform compatibility.  
SMBLibrary shares can be accessed from any Windows version since Windows NT 4.0.  

Supported SMB / CIFS transport methods:
=======================================
• NetBIOS over TCP (port 139)  
• Direct TCP hosting (port 445)

###### 'NetBIOS over TCP' and 'Direct TCP hosting' are almost identical, the only differences:
- A 'session request' packet is initiating the NBT connection.
- A 'keep alive' packet is sent from time to time over NBT connections.
- SMB2: Direct TCP hosting supports large MTUs.

Notes:
======
By default, Windows already use ports 139 and 445. there are several techniques to free / utilize those ports:

### Method 1: Disable Windows File and Printer Sharing server completely:
###### Windows XP/2003:
1. For every network adapter: Uncheck 'File and Printer Sharing for Microsoft Networks".
2. Navigate to 'HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetBT\Parameters' and set 'SMBDeviceEnabled' to '0' (this will free port 445).
3. Reboot.

###### Windows 7/8/2008/2012:
Disable the "Server" service (p.s. "TCP\IP NETBIOS Helper" should be enabled).

### Method 2: Use Windows File Sharing AND SMBLibrary:
Windows bind port 139 to the first IP addres of every adapter, while port 445 is bound globally.
This means that if you'll disable port 445 (or block it using a firewall), you'll be able to use a different service on port 139 for every IP address.

###### Additional Notes:
* To free port 139 for a given adapter, go to 'Internet Protocol (TCP/IP) Properties' > Advanced > WINS, and select 'Disable NetBIOS over TCP/IP'.
Uncheck 'File and Printer Sharing for Microsoft Networks' to ensure Windows will not answer to SMB traffic on port 445 for this adapter.

* It's important to note that disabling NetBIOS over TCP/IP will also disable NetBIOS name service for that adapter (a.k.a. WINS), This service uses UDP port 137.
SMBLibrary offers a name service of its own.

* You can install a virtual network adapter driver for Windows to be used solely with SMBLibrary:
  - You can install the 'Microsoft Loopback adapter' and use it for server-only communication with SMBLibrary.

###### Windows 7/8/2008/2012:
* It's possible to prevent Windows from using port 445 by removing all of the '\Device\Tcpip_{..}' and '\Device\Tcpip6_{..}' entries from the `Bind' registry key under 'HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\LanmanServer\Linkage'.  

* if you want localhost access from Windows explorer to work as expected, you must specify the IP address that you selected (\\\\127.0.0.1 or \\\\localhost will not work as expected), in addition, I have observed that when connecting to the first IP address of a given adapter, Windows will only attempt to connect to port 445.

### Method 3: Reserve port 445 as startup:

Open a console as admin

Allow binding to 127.0.0.2

`netsh interface ipv4 add address "Loopback Pseudo-Interface 1" 127.0.0.2`

The following steps configure Port Forwarding to run before LAN Man Service, which will let us reserve 127.0.0.2:445 for our own use.

Inspect the current dependencies for 'Server' service

`sc qc lanmanserver`

Add 'iphlpsvc' as a dependency. Note - the space after 'depend' is important.

`sc config lanmanserver depend= samss/srv2/iphlpsvc`

Create a Port Forward between 127.0.0.2:445 and 127.0.0.1:44500. (Since the Port Forwarding Service now runs first, it means it'll be reserved for our use and not automatically taken by the Windows SMB Component).

`netsh interface portproxy add v4tov4 listenaddress=127.0.0.2 listenport=445 connectaddress=127.0.0.1 connectport=44500`

Reboot, check:

`netstat -an | find ":445"`

Expect:

`TCP    127.0.0.2:445          0.0.0.0:0              LISTENING`

You can now run your SMB server to listen on `127.0.0.1:44500`

And you can browse its files by opening explorer to:

`\\127.0.0.2`


### Method 4: Use an IP address that is invisible to Windows File Sharing:
Using PCap.Net you can programmatically setup a virtual Network adapter and intercept SMB traffic (similar to how a virtual machine operates), You should use the ARP protocol to notify the network about the new IP address, and then process the incoming SMB traffic using SMBLibrary, good luck!

### Method 5: Use Windows 11 (version 24H2 or later) which supports SMB alternative ports:

Run your SMB server on a custom port (eg. 44500)

Map it to a drive letter:

`NET USE S: \\127.0.0.1\share /TCPPORT:44500`

Browse the drive letter using Windows Explorer.

Source: [Microsoft](https://learn.microsoft.com/en-us/windows-server/storage/file-server/smb-ports?tabs=command-line#map-an-alternative-port)

Using SMBLibrary:
=================
Any directory / filesystem / object you wish to share must implement the IFileSystem interface (or the lower-level INTFileStore interface).  
You can share anything from actual directories to custom objects, as long as they expose a directory structure.  

Client code examples can be found [here](ClientExamples.md).

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
