SMBLibrary Server Notes:
========================
By default, Windows already use ports 139 and 445. there are several techniques to free / utilize those ports:

##### Method 1: Disable Windows File and Printer Sharing server completely:
###### Windows XP/2003:
1. For every network adapter: Uncheck 'File and Printer Sharing for Microsoft Networks".
2. Navigate to 'HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NetBT\Parameters' and set 'SMBDeviceEnabled' to '0' (this will free port 445).
3. Reboot.

###### Windows 7/8/2008/2012:
Disable the "Server" service (p.s. "TCP\IP NETBIOS Helper" should be enabled).

##### Method 2: Use Windows File Sharing AND SMBLibrary:
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

##### Method 3: Use an IP address that is invisible to Windows File Sharing:
Using PCap.Net you can programmatically setup a virtual Network adapter and intercept SMB traffic (similar to how a virtual machine operates), You should use the ARP protocol to notify the network about the new IP address, and then process the incoming SMB traffic using SMBLibrary, good luck! 

Using SMBLibrary Server implementation:
=======================================
Any directory / filesystem / object you wish to share must implement the IFileSystem interface (or the lower-level INTFileStore interface).  
You can share anything from actual directories to custom objects, as long as they expose a directory structure.  
