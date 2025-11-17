# SMB and DFS Test Lab Setup (for SMBLibrary)

This guide shows how to stand up a reproducible lab to test SMB and DFS with this repository. It offers three paths:

- Minimal local SMB server using this repo’s `SMBServer` app (no DFS)
- Windows Server DFS Namespaces (recommended for DFS referrals)
- Optional Linux Samba DFS (msdfs) for cross‑platform testing

Each section includes validation steps and hardening notes with citations.

---

## 1) What you can test with this repo

- **SMB client basics**: The tests in `SMBLibrary.Tests` (e.g., `IntegrationTests/LoginTests.cs`) connect to a server over loopback and validate login/logout behavior.
- **Local SMB server**: The WinForms `SMBServer` app in `SMBServer/` reads `Settings.xml` to define users and shares, and can run SMB1/SMB2 over NetBIOS or TCP 445.
- **DFS client scenarios**: This repo includes types for DFS referral requests/responses, but the included `SMBServer` app is a basic file server and does not act as a DFS Namespace server. For referral testing, use a Windows DFS Namespace or Samba `msdfs`.

---

## 2) Minimal local SMB test (no DFS)

Use the built‑in server to validate SMB client behavior quickly on your dev workstation.

- Build and run `SMBServer` (WinForms)
  - Open `SMBServer.sln` in Visual Studio. Set startup project to `SMBServer` and run.
  - In the UI, pick an IP (`Any` is fine), choose transport (Direct TCP is port 445), and uncheck SMB1 if not needed.
- Configure users and shares
  - Edit `SMBServer/Settings.xml`:
    - Users are in `<Users>`; passwords are plain text for lab use, e.g., `Admin/admin`, `Test/test`.
    - Shares are in `<Shares>` with Windows paths, e.g., `C:\Shared`. Create the folder and adjust read/write `Accounts`.
  - Start the server from the UI.
- Validate from Windows
  - File Explorer: `\\<host>\Shared` or PowerShell: `Test-Path \\<host>\Shared`.
  - If using the repo’s tests, see `SMBLibrary.Tests/IntegrationTests/LoginTests.cs` for a loopback login example.

Hardening (local): disable SMB1 in the app UI and prefer SMB2/3 (see Security notes below).

---

## 3) Windows Server DFS Namespace lab (recommended for DFS)

A small domain‑based DFS lab (works with Server 2019/2022/2025):

- 1x Domain Controller (DC)
- 1x DFS Namespace server (can be the DC for a lab)
- 2x File servers hosting real shares (folder targets)

You can also deploy a standalone DFS namespace without AD, hosted on one server.

### 3.1 Install roles and tools

On the DFS Namespace server (and optionally on file servers), install:

```powershell
# DFS Namespace server + DFS Management tools
Install-WindowsFeature "FS-DFS-Namespace","RSAT-DFS-Mgmt-Con"

# Optional: DFS Replication tools/service if you plan to replicate folder targets
Install-WindowsFeature FS-DFS-Replication
```

References: [DFS overview – Install DFS Namespaces][ms-dfs-overview], [Install DFS Replication][ms-dfsr-install].

### 3.2 Prepare target shares on the file servers

On FS1 and FS2, create NTFS folders and shares, e.g., `\\FS1\Tools`, `\\FS2\Tools`.

```powershell
New-Item -Path 'D:\Data\Tools' -ItemType Directory
New-SmbShare -Name 'Tools' -Path 'D:\Data\Tools' -FullAccess 'DOMAIN\\FileAdmins' -ChangeAccess 'DOMAIN\\Users'
```

### 3.3 Create the namespace

Pick domain‑based (recommended) or stand‑alone.

Create the local namespace root folder and share on the DFSN server:

```powershell
$nsName = 'Public'
$nsRoot = "C:\\DFSRoots\\$nsName"
New-Item -Path $nsRoot -ItemType Directory -Force | Out-Null
New-SmbShare -Name $nsName -Path $nsRoot -FullAccess 'DOMAIN\\FileAdmins'
```

Create the namespace:

```powershell
# Domain-based (recommended)
New-DfsnRoot -Path "\\\\contoso.com\\$nsName" -TargetPath "\\\\DFSN1\\$nsName" -Type DomainV2

# OR Standalone
# New-DfsnRoot -Path "\\\\DFSN1\\$nsName" -TargetPath $nsRoot -Type Standalone
```

References: [Create a DFS namespace][ms-create-namespace], [Choose a namespace type][ms-choose-type].

### 3.4 Add folders and folder targets

Add a DFS folder named `Software\Tools` with targets on FS1 and FS2:

```powershell
$dfsnPath = "\\\\contoso.com\\$nsName\\Software\\Tools"
New-DfsnFolder -Path $dfsnPath -TargetPath "\\\\FS1\\Tools"
New-DfsnFolderTarget -Path $dfsnPath -TargetPath "\\\\FS2\\Tools"
```

### 3.5 Optional: enable replication between targets

Use DFS Management: right‑click the folder, choose Replicate, and follow the wizard. Or script with DFSR PowerShell. References: [Replicate folder targets using DFSR][ms-replicate-folder], [DFS Replication overview][ms-dfsr-overview].

### 3.6 Secure the namespace and SMB

- Secure the namespace root folder (permissions, disable inheritance as needed) [Secure the namespace][ms-secure-namespace].
- Disable SMB1 and use SMB 3.1.1. Consider signing or encryption:
  - Prefer encryption for privacy + integrity; signing for integrity only [SMB security][ms-smb-security], [SMB signing overview][ms-smb-signing].
  - Optionally require client encryption (Windows 11 24H2+/Server 2025+) [Client require encryption][ms-smb-client-encrypt].

Example (lab, optional):

```powershell
# Server: remove SMB1 feature (Windows Server)
Remove-WindowsFeature FS-SMB1

# Client: set SMB 3.1.1 min/max (lab only – understand impact first)
Set-ItemProperty -Path "HKLM:\\SYSTEM\\CurrentControlSet\\Services\\LanmanWorkstation\\Parameters" -Name "MinSMB2Dialect" -Value 0x000000311
Set-ItemProperty -Path "HKLM:\\SYSTEM\\CurrentControlSet\\Services\\LanmanWorkstation\\Parameters" -Name "MaxSMB2Dialect" -Value 0x000000311
```

Security references: [Protect SMB from interception][ms-smb-interception], [SMB security hardening][ms-smb-hardening].

### 3.7 Validate DFS

From a domain‑joined Windows client:

```powershell
# List namespaces/folders/targets
Get-DfsnRoot
Get-DfsnFolder -Path "\\\\contoso.com\\Public\\Software\\Tools"
Get-DfsnFolderTarget -Path "\\\\contoso.com\\Public\\Software\\Tools"

# Mount and access via DFS
New-PSDrive -Name X -PSProvider FileSystem -Root "\\\\contoso.com\\Public\\Software\\Tools" -Persist
Test-Path X:\\

# Inspect referrals
dfsutil /pktinfo
```

If access fails, clear referral cache and retry:

```powershell
dfsutil cache referral flush
```

---

## 4) Optional: Samba (Linux) DFS (msdfs) lab

For cross‑platform referral testing without Windows DFSN, Samba can host a DFS root that redirects to Windows shares.

### 4.1 Install Samba and enable msdfs

Example (Debian/Ubuntu):

```bash
sudo apt-get update && sudo apt-get install -y samba
```

`/etc/samba/smb.conf` (minimal):

```ini
[global]
   workgroup = CONTOSO
   security = user
   host msdfs = yes

[dfsroot]
   path = /srv/samba/dfsroot
   msdfs root = yes
   browseable = yes
   read only = no
```

Create DFS root and links:

```bash
sudo mkdir -p /srv/samba/dfsroot
sudo chown -R root:root /srv/samba/dfsroot
# Example referral to two targets (Windows file shares)
# The link name appears as a folder under \\samba\\dfsroot
sudo ln -s "msdfs:FS1\\Tools,FS2\\Tools" /srv/samba/dfsroot/Tools
sudo systemctl restart smbd
```

References: [Samba DFS wiki][samba-dfs-wiki], [Samba HOWTO – Hosting a DFS tree][samba-howto-msdfs].

Validate from Windows: `\\samba\dfsroot\Tools` should transparently redirect to one of the targets.

---

## 5) How to exercise this repo against the lab

- Use `SMBServer` for local SMB protocol testing (connect to `\\<host>\<share>`). Adjust users/shares via `Settings.xml`.
- For DFS resolution:
  - Windows clients and tools (Explorer, `New-PSDrive`, `dfsutil`) will resolve DFS paths automatically.
  - If your custom client does not implement DFS referral handling, resolve the DFS path to a concrete target first (e.g., via OS, `Get-DfsnFolderTarget`, or by inspecting `dfsutil /pktinfo`) and then connect directly to the target share (`\\FS1\Tools`).

---

## 6) Troubleshooting checklist

- Ensure DNS/AD are healthy (for domain‑based namespaces).
- Verify DFSN cmdlets and GUI show expected roots, folders, and targets.
- Check share/NTFS ACLs and namespace root folder ACLs.
- Flush DFS referral cache (`dfsutil cache referral flush`).
- Confirm SMB dialect compatibility and that SMB1 is disabled.
- If using DFS Replication, allow time for AD/DFSR convergence or force polling per docs.

---

## 7) VM-based lab options (Windows Server in VM or Azure)

You can host the DFS Namespace on a Windows Server VM instead of physical hardware. Two practical options:

- Local VM on your workstation (e.g., Windows Server evaluation in VirtualBox/VMware/Hyper‑V)
- Azure Windows Server VM (pay‑as‑you‑go)

### 7.1 Local Windows Server VM (works with Windows 11 Home or Pro)

- **Hypervisor choices**
  - Windows 11 Home: use VirtualBox or VMware Workstation Player.
  - Windows 11 Pro: you can also use Hyper‑V.
- **Download media**
  - Download a Windows Server 2019/2022 ISO from Microsoft (evaluation editions are typically time‑limited but free to use for lab/testing).
- **Suggested VM sizing (lab)**
  - 2 vCPUs.
  - 4–8 GB RAM (4 GB minimum; 8 GB is more comfortable if also running AD + DFS + file services).
  - 60–80 GB virtual disk.
- **High‑level setup steps**
  - Create a new VM, attach the Windows Server ISO, and install the OS.
  - Configure a private or NAT virtual network so only your host and VM can talk.
  - Enable Remote Desktop so you can RDP into the VM from your host.
  - (Optional but convenient) Promote the server to a domain controller and create a small lab domain (for domain‑based DFS namespaces).
  - Install DFS roles and follow section **3) Windows Server DFS Namespace lab** inside the VM.
- **Cost considerations**
  - Hypervisor and evaluation ISO: typically free for non‑production testing.
  - The main cost is host hardware resources (RAM/CPU/disk) and your time; there is no per‑hour cloud cost.

### 7.2 Azure Windows Server VM

If you prefer not to run local VMs, you can host the DFS Namespace in an Azure Windows Server VM.

- **Basic setup outline**
  - In the Azure portal, create a small Windows Server VM (for example, a `B2s` or similar size) in a test subscription.
  - Allow RDP inbound (TCP 3389) from your IP so you can administer the VM.
  - Inside the VM, optionally configure AD DS for a small domain, then install DFS Namespaces/DFS Replication roles and follow section **3)** as you would on‑prem.
  - For DFS/SMB testing, the safest pattern is to run tests **inside the VM** (RDP in, open your repo, run tests there) instead of exposing SMB (TCP 445) to the internet.
- **Connectivity from your machine (optional)**
  - If you must access DFS from your local machine, set up a secure channel such as an Azure point‑to‑site VPN or Azure Bastion + RDP, and keep SMB ports closed to the public internet.
- **Cost considerations**
  - Azure VM costs depend on size, region, OS, and uptime (billed per second/minute).
  - A small dev/test VM (for example, a burstable `B2s` class) is typically on the order of **tens of USD per month** if run 24/7; running only a few hours per day can reduce this significantly.
  - Use the official Azure pricing calculator to estimate costs for your region and usage pattern:
    - https://azure.microsoft.com/pricing/calculator/

---

## References

- [DFS Namespaces overview][ms-dfs-overview]
- [Create a DFS namespace][ms-create-namespace]
- [Choose a namespace type][ms-choose-type]
- [Replicate folder targets using DFS Replication][ms-replicate-folder]
- [Install DFS Replication][ms-dfsr-install] and [DFS Replication overview][ms-dfsr-overview]
- [Secure the namespace][ms-secure-namespace]
- [SMB security (encryption/signing)][ms-smb-security] and [SMB signing overview][ms-smb-signing]
- [Protect SMB traffic from interception][ms-smb-interception]
- [Configure SMB client to require encryption][ms-smb-client-encrypt]
- [Samba DFS wiki][samba-dfs-wiki] and [Samba HOWTO – Hosting a DFS tree][samba-howto-msdfs]

[ms-dfs-overview]: https://learn.microsoft.com/en-us/windows-server/storage/dfs-namespaces/dfs-overview
[ms-create-namespace]: https://learn.microsoft.com/en-us/windows-server/storage/dfs-namespaces/create-a-dfs-namespace
[ms-choose-type]: https://learn.microsoft.com/en-us/windows-server/storage/dfs-namespaces/choose-a-namespace-type
[ms-replicate-folder]: https://learn.microsoft.com/en-us/windows-server/storage/dfs-namespaces/replicate-folder-targets-using-dfs-replication
[ms-dfsr-install]: https://learn.microsoft.com/en-us/windows-server/storage/dfs-replication/install-dfs-replication
[ms-dfsr-overview]: https://learn.microsoft.com/en-us/windows-server/storage/dfs-replication/dfs-replication-overview
[ms-secure-namespace]: https://learn.microsoft.com/en-us/windows-server/storage/dfs-namespaces/create-a-dfs-namespace#secure-the-namespace
[ms-smb-security]: https://learn.microsoft.com/en-us/windows-server/storage/file-server/smb-security
[ms-smb-signing]: https://learn.microsoft.com/en-us/windows-server/storage/file-server/smb-signing-overview
[ms-smb-interception]: https://learn.microsoft.com/en-us/windows-server/storage/file-server/smb-interception-defense
[ms-smb-client-encrypt]: https://learn.microsoft.com/en-us/windows-server/storage/file-server/configure-smb-client-require-encryption
[samba-dfs-wiki]: https://wiki.samba.org/index.php/Distributed_File_System_(DFS)
[samba-howto-msdfs]: https://www.samba.org/samba/docs/old/Samba3-HOWTO/msdfs.html

---

## 8) Suggested test matrix (SMBLibrary vs lab)

Use this as a checklist when iterating on SMBLibrary’s DFS‑related behavior.

- **Baseline SMB (no DFS)**
  - SMB2/3 dialect negotiation against `SMBServer` and against Windows/Samba file shares.
  - Basic CRUD, rename, delete, directory enumeration (verify timestamps/attributes).
  - Signing/encryption combinations that your environment supports.

- **DFS resolution via Windows OS**
  - From a Windows client, map a DFS path (`\\contoso.com\\Public\\Software\\Tools`) to a drive.
  - Point SMBLibrary’s client to the resolved target (`\\FS1\\Tools` or `\\FS2\\Tools`) rather than the DFS path.
  - Validate that failover/Load‑balancing is visible via `dfsutil /pktinfo`, even though SMBLibrary itself does not yet issue DFS referral IOCTLs.

- **DFS referral IOCTL scaffolding**
  - Capture DFS referral traffic with a network analyzer (e.g., Wireshark) while a Windows client resolves the DFS path.
  - Compare frames to SMBLibrary’s `DFS/RequestGetDfsReferral` and `ResponseGetDfsReferral` structures.
  - Note gaps (unimplemented fields, version support) as input for future codec work.

- **ChangeNotify with DFS targets**
  - On a DFS folder that points to a Windows share, exercise SMBLibrary’s `ChangeNotify` implementation against the concrete target path.
  - Validate: `STATUS_PENDING` interim, completion on change, cancellation behavior, buffer‑overflow handling.

- **Security hardening checks**
  - Confirm that disabling SMB1 in the lab does not break SMBLibrary’s SMB2+ scenarios.
  - Verify that event logs and SMBLibrary logging do not capture raw DFS payloads or secrets.

Record findings and packet captures under `docs/labs/` or a similar folder so future DFS work has concrete examples to regress against.
