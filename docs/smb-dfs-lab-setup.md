# SMB + DFS Lab Environment (Hyper-V + Windows Server 2022)

This guide describes how to build a small lab for testing SMB and DFS using Hyper‑V and Windows Server 2022.

---

## 1. Lab overview

### 1.1 Objectives

- **Test SMB** access to file shares.
- **Experiment with DFS Namespaces** (domain‑based) and **DFS Replication**.
- **Validate failover behavior** when one file server is unavailable.

### 1.2 Topology

**Network**

- **Hyper‑V virtual switch**: `LAB_Internal` (Internal switch)
- **Subnet**: `10.0.0.0/24`
- **Host vNIC on LAB_Internal**: `10.0.0.1/24`

**Servers (all Windows Server 2022)**

- **LAB-DC1**
  - Role: Domain Controller + DNS
  - IP: `10.0.0.10`
  - DNS: `10.0.0.10`
  - Domain: `LAB.LOCAL` (new forest)
  - Suggested: 2 vCPU, 4 GB RAM, 60+ GB disk

- **LAB-FS1**
  - Role: File server + DFS Namespace server + DFS Replication member
  - IP: `10.0.0.20`
  - DNS: `10.0.0.10`
  - Member of `LAB.LOCAL`
  - Suggested: 2 vCPU, 4 GB RAM, 80+ GB disk

- **LAB-FS2**
  - Role: File server + DFS Namespace server + DFS Replication member
  - IP: `10.0.0.21`
  - DNS: `10.0.0.10`
  - Member of `LAB.LOCAL`
  - Suggested: 2 vCPU, 4 GB RAM, 80+ GB disk

> You can start with just `LAB-DC1` + `LAB-FS1` and add `LAB-FS2` later if resources are tight.

---

## 2. Prerequisites

- Hyper‑V installed and enabled on the host.
- Windows Server 2022 ISO available to Hyper‑V.
- Sufficient host resources (ideally ≥ 16 GB RAM, 4+ cores).

---

## 3. Create the Hyper‑V internal switch

1. Open **Hyper‑V Manager** on the host.
2. In the right pane, select the host, then click **Virtual Switch Manager…**.
3. Under **Create virtual switch**, select **Internal**.
4. Click **Create Virtual Switch**.
5. Name the switch **`LAB_Internal`**.
6. Click **OK**.

### 3.1 Configure host vNIC on LAB_Internal

1. Open **Control Panel → Network and Internet → Network Connections**.
2. Locate the adapter named `vEthernet (LAB_Internal)`.
3. Open **Properties → Internet Protocol Version 4 (TCP/IPv4)**.
4. Set:
   - IP address: `10.0.0.1`
   - Subnet mask: `255.255.255.0`
   - Default gateway: (leave blank)
   - DNS servers: (leave blank for now, or later `10.0.0.10` if you want the host to use lab DNS)
5. Click **OK**.

---

## 4. Create the virtual machines

Repeat these steps for `LAB-DC1`, `LAB-FS1`, and `LAB-FS2`.

1. In **Hyper‑V Manager**, right‑click the host → **New → Virtual Machine…**.
2. Name the VM appropriately (e.g. `LAB-DC1`).
3. Specify **Generation 2** (recommended for Server 2022).
4. Assign startup memory (e.g. **4096 MB**).
5. Connect the network to **`LAB_Internal`**.
6. Create a virtual hard disk (60–80 GB or more for file servers).
7. Installation options: select **Install an operating system from a bootable image file** and browse to the **Windows Server 2022 ISO**.
8. Finish the wizard and start the VM.
9. Install Windows Server normally:
   - Choose **Desktop Experience** if you want a GUI.
   - Set the local Administrator password.
10. After installation, ensure the VM boots from disk (detach ISO from DVD drive if necessary).

---

## 5. Configure LAB-DC1 (AD DS + DNS)

### 5.1 Rename and assign static IP

Inside `LAB-DC1`:

1. Log in as local Administrator.
2. Open **System Properties** (e.g. `sysdm.cpl`) and rename the computer to `LAB-DC1`, reboot when prompted.
3. Configure the NIC:
   - IP address: `10.0.0.10`
   - Subnet mask: `255.255.255.0`
   - Default gateway: (blank, unless you need internet)
   - Preferred DNS server: `10.0.0.10`

### 5.2 Install AD DS and DNS roles

1. Open **Server Manager**.
2. Click **Add roles and features**.
3. Role‑based or feature‑based installation → Next.
4. Select `LAB-DC1` as the target server.
5. Under **Server Roles**, select:
   - **Active Directory Domain Services**
   - **DNS Server**
6. Accept required features and complete the wizard.

### 5.3 Promote LAB-DC1 to domain controller

1. In **Server Manager**, click the **yellow notification flag**.
2. Choose **Promote this server to a domain controller**.
3. In the wizard:
   - **Add a new forest**.
   - Root domain name: `LAB.LOCAL`.
   - Set a DSRM password when prompted.
   - Accept defaults for DNS and paths.
4. Complete the wizard and reboot.

At this point, `LAB.LOCAL` exists with `LAB-DC1` as the first DC and DNS server.

---

## 6. Configure LAB-FS1 and LAB-FS2 (domain members)

Repeat for `LAB-FS1` and `LAB-FS2`.

### 6.1 Rename and assign static IP

On `LAB-FS1`:

1. Rename computer to `LAB-FS1`, reboot.
2. Configure NIC:
   - IP address: `10.0.0.20`
   - Subnet mask: `255.255.255.0`
   - Default gateway: (blank, unless needed)
   - Preferred DNS server: `10.0.0.10`

On `LAB-FS2`:

1. Rename computer to `LAB-FS2`, reboot.
2. Configure NIC:
   - IP address: `10.0.0.21`
   - Subnet mask: `255.255.255.0`
   - Default gateway: (blank, unless needed)
   - Preferred DNS server: `10.0.0.10`

### 6.2 Join the domain LAB.LOCAL

On each file server:

1. Open **System Properties → Computer Name → Change…**.
2. Select **Domain** and enter `LAB.LOCAL`.
3. Authenticate as `LAB\Administrator` (or another domain admin).
4. Reboot.

Quick connectivity checks (from each file server):

```powershell
ping LAB-DC1
nslookup LAB-DC1
```

---

## 7. Install File Services and DFS roles

On **LAB-FS1** and **LAB-FS2**:

1. Open **Server Manager**.
2. Click **Add roles and features**.
3. Role‑based or feature‑based installation → Next.
4. Select the server (`LAB-FS1` or `LAB-FS2`).
5. Under **File and Storage Services → File and iSCSI Services**, select:
   - **File Server**
   - **DFS Namespaces**
   - **DFS Replication**
6. Complete the wizard and reboot if required.

---

## 8. Create SMB shares for DFS targets

On each file server, create a folder and share it.

Example using `D:` (adjust if you only have `C:`):

1. Create folder `D:\Data\Sales` (or `C:\Data\Sales`).
2. Right‑click the folder → **Properties → Sharing → Advanced Sharing…**.
3. Check **Share this folder**.
4. Share name: `Sales`.
5. Click **Permissions** and grant access as needed (for lab, `Everyone: Read` or `Change`).
6. Click **OK** to close dialogs.

Repeat on `LAB-FS1` and `LAB-FS2` so that:

- `\\LAB-FS1\Sales`
- `\\LAB-FS2\Sales`

are both valid SMB shares.

Basic access test (from another server, e.g. `LAB-DC1`):

```powershell
Test-Path \\LAB-FS1\Sales
Test-Path \\LAB-FS2\Sales
```

---

## 9. Configure DFS Namespace

Use **DFS Management** (`dfsmgmt.msc`) on `LAB-DC1` (or any server with the DFS tools installed).

### 9.1 Create a domain-based namespace

1. Open **DFS Management**.
2. Right‑click **Namespaces** → **New Namespace…**.
3. For **Server**, enter `LAB-FS1` (first namespace server).
4. Namespace name: `Files`.
5. The resulting UNC will be `\\LAB.LOCAL\Files`.
6. Choose **Domain‑based namespace** and keep Windows Server 2008 mode enabled (default).
7. Finish the wizard.

### 9.2 Add additional namespace server

1. In DFS Management, select the namespace `\\LAB.LOCAL\Files`.
2. Right‑click it → **Add Namespace Server…**.
3. Add `LAB-FS2`.

Now both `LAB-FS1` and `LAB-FS2` host the namespace.

### 9.3 Add DFS folder and targets

Create a DFS folder `Sales` with two targets.

1. Right‑click namespace `\\LAB.LOCAL\Files` → **New Folder…**.
2. Name: `Sales`.
3. Click **Add…** and add targets:
   - `\\LAB-FS1\Sales`
   - `\\LAB-FS2\Sales`
4. Confirm and close.

The DFS path for clients is now: `\\LAB.LOCAL\Files\Sales`.

---

## 10. Configure DFS Replication

When adding multiple folder targets, the wizard may offer to create a replication group automatically. If it does, accept and use the settings below. If not, create it manually.

### 10.1 Create replication group for the Sales folder

1. In **DFS Management**, right‑click **Replication** → **New Replication Group for DFS Namespace…**.
2. Select the namespace folder `\\LAB.LOCAL\Files\Sales`.
3. Add members:
   - `LAB-FS1`
   - `LAB-FS2`
4. Topology: **Full Mesh**.
5. Replication schedule: **Full**.
6. Choose a **primary member** (e.g. `LAB-FS1`) whose content will seed the other member.
7. Finish the wizard.

Give replication a few minutes to initialize, especially on first setup.

---

## 11. Validation tests

### 11.1 Basic SMB access

From any domain‑joined machine (e.g. `LAB-DC1`):

```powershell
# Direct server shares
Test-Path \\LAB-FS1\Sales
Test-Path \\LAB-FS2\Sales

# DFS namespace
Test-Path \\LAB.LOCAL\Files\Sales
```

Using File Explorer, open:

- `\\LAB-FS1\Sales`
- `\\LAB-FS2\Sales`
- `\\LAB.LOCAL\Files\Sales`

Create a test file on each share:

- On `\\LAB-FS1\Sales`: `FS1.txt`
- On `\\LAB-FS2\Sales`: `FS2.txt`

After DFS Replication has time to sync, you should see both `FS1.txt` and `FS2.txt` in `\\LAB.LOCAL\Files\Sales`.

### 11.2 DFS target resolution

From a domain‑joined machine:

```cmd
dfsutil /pktinfo
```

- Verify which target (`\\LAB-FS1\Sales` or `\\LAB-FS2\Sales`) the DFS client is using for `\\LAB.LOCAL\Files\Sales`.

### 11.3 Failover test

1. Note which file server is currently serving the DFS path (see `dfsutil /pktinfo`).
2. Simulate failure on that server:
   - Option A: Stop the **Server** service (`LanmanServer`).
   - Option B: Shut down the VM (`LAB-FS1` or `LAB-FS2`).
3. On the client, access `\\LAB.LOCAL\Files\Sales` again and verify it fails over to the remaining target.
4. Bring the failed server back online and confirm access remains consistent.

### 11.4 Host OS access (optional)

On the host’s `LAB_Internal` vNIC, set DNS to `10.0.0.10`. Then from the host OS:

- Access `\\LAB.LOCAL\Files\Sales` in File Explorer.
- Run `nslookup LAB-DC1` to confirm DNS resolution from the lab DC.

---

## 12. Optional extensions

- Add a Windows 10/11 client VM joined to `LAB.LOCAL` for more realistic SMB client testing.
- Create additional DFS folders (e.g. `\\LAB.LOCAL\Files\Engineering`, `\\LAB.LOCAL\Files\Finance`).
- Test different share/NTFS permission combinations and access control scenarios.
- Add a second domain controller for redundancy and test AD/DFS behavior with multi‑DC setups.

---

## 13. Detailed test scenarios

This section groups concrete scenarios you can run in the lab. Use a domain‑joined client (e.g. `LAB-DC1` or a Windows 10/11 VM) unless stated otherwise.

### 13.1 SMB connectivity and dialects

- **Scenario: Verify SMB connectivity to each file server**

  1. From a client, run:

     ```powershell
     Test-Path \\LAB-FS1\Sales
     Test-Path \\LAB-FS2\Sales
     Test-Path \\LAB.LOCAL\Files\Sales
     ```

  2. Confirm all three paths return `True`.

- **Scenario: Inspect SMB sessions and dialects**

  1. On the client, open the DFS path in File Explorer: `\\LAB.LOCAL\Files\Sales`.
  2. On the client, run:

     ```powershell
     Get-SmbConnection | Where-Object { $_.ServerName -like 'LAB-FS*' } | Format-Table -AutoSize
     ```

  3. Observe the `Dialect` column (e.g. `3.1.1`) and confirm connections are established to one of the file servers.

- **Scenario: Compare direct server vs DFS namespace connections**

  1. Map a drive to `LAB-FS1` directly:

     ```powershell
     New-PSDrive -Name FS1 -PSProvider FileSystem -Root \\LAB-FS1\Sales -Persist
     ```

  2. Map a drive to the DFS namespace:

     ```powershell
     New-PSDrive -Name DFS -PSProvider FileSystem -Root \\LAB.LOCAL\Files\Sales -Persist
     ```

  3. Compare `Get-SmbConnection` output for both connections.

### 13.2 DFS namespace and referrals

- **Scenario: View client referral cache (PKT)**

  1. On the client, access `\\LAB.LOCAL\Files\Sales`.
  2. Run:

     ```cmd
     dfsutil /pktinfo
     ```

  3. Identify which target (e.g. `\\LAB-FS1\Sales`) is being used for the DFS path.

- **Scenario: Refresh referrals and test target selection**

  1. Clear the referral cache:

     ```cmd
     dfsutil /pktflush
     ```

  2. Re‑access `\\LAB.LOCAL\Files\Sales`.
  3. Run `dfsutil /pktinfo` again and confirm a new referral was obtained.

- **Scenario: Client behavior when one target is unavailable**

  1. Determine which target is currently used via `dfsutil /pktinfo`.
  2. On that file server (e.g. `LAB-FS1`), stop the **Server** service or shut down the VM.
  3. On the client, attempt to browse `\\LAB.LOCAL\Files\Sales`.
  4. Observe failover to the remaining target after a short delay.
  5. Bring the failed server back and flush the PKT (`dfsutil /pktflush`), then re‑access the namespace.

### 13.3 DFS Replication behavior

- **Scenario: Replication of new files**

  1. On `LAB-FS1`, create `D:\Data\Sales\from-fs1.txt`.
  2. On `LAB-FS2`, create `D:\Data\Sales\from-fs2.txt`.
  3. After a short delay, verify both files exist on both servers.

- **Scenario: Replication latency measurement**

  1. On `LAB-FS1`, create a file with a timestamped name, e.g. `D:\Data\Sales\latency-<timestamp>.txt`.
  2. Note the creation time.
  3. On `LAB-FS2`, refresh the folder view periodically until the file appears.
  4. Calculate approximate replication delay.

- **Scenario: Conflicting edits**

  1. Ensure DFS replication is healthy.
  2. On `LAB-FS1`, create `D:\Data\Sales\conflict.txt` with some content.
  3. Allow it to replicate to `LAB-FS2`.
  4. Modify `conflict.txt` on both servers nearly simultaneously (e.g. different text).
  5. After replication converges, review the file contents and the DFS Replication conflict/loser files folder to see which version won.

- **Scenario: Replication when a server is offline**

  1. Shut down `LAB-FS2`.
  2. On `LAB-FS1`, create several new files and modify existing ones in `D:\Data\Sales`.
  3. Start `LAB-FS2` again.
  4. After some time, verify that all changes from `LAB-FS1` are replicated to `LAB-FS2`.

### 13.4 Authentication and authorization

- **Preparation**

  1. On `LAB-DC1`, create a few test users and groups:
     - `LAB\\UserRead`
     - `LAB\\UserModify`
     - Group `LAB\\Sales-Read`
     - Group `LAB\\Sales-Modify`

  2. Add `UserRead` to `Sales-Read` and `UserModify` to `Sales-Modify`.

- **Scenario: Share vs NTFS permissions (allow)**

  1. On `LAB-FS1` and `LAB-FS2`, set the **share permissions** on `Sales` to allow `Everyone: Full Control` (lab only).
  2. On the NTFS **Security** tab for `D:\Data\Sales` on both servers:
     - Grant `Sales-Read` **Read & execute**.
     - Grant `Sales-Modify` **Modify**.
  3. Log on as `LAB\\UserRead` and attempt to create/modify files in `\\LAB.LOCAL\Files\Sales`.
  4. Log on as `LAB\\UserModify` and repeat.
  5. Confirm effective permissions match expectations.

- **Scenario: Share vs NTFS permissions (deny)**

  1. On NTFS permissions, explicitly **Deny** `Sales-Read` **Write**.
  2. Test again as `UserRead` and confirm writes fail while reads succeed.
  3. Optionally, set share permissions to restrict access further and observe combined effects.

### 13.5 Client caching and offline behavior (optional)

- **Scenario: Client-side caching / Offline Files**

  1. On a Windows client, enable Offline Files (if supported) and mark `\\LAB.LOCAL\Files\Sales` as available offline.
  2. Create or modify files while online.
  3. Simulate loss of connectivity (disconnect client from `LAB_Internal`).
  4. Access cached files and then reconnect.
  5. Observe synchronization behavior.

### 13.6 Multi-path and troubleshooting commands

- **Scenario: Inspect SMB shares and sessions on the server**

  1. On `LAB-FS1` or `LAB-FS2`, run:

     ```powershell
     Get-SmbShare
     Get-SmbSession
     ```

  2. Confirm sessions appear from your client when accessing DFS and direct paths.

- **Scenario: Clear SMB connections and DFS cache**

  1. On the client, close all Explorer windows and unmap any test drives.
  2. Run:

     ```powershell
     Get-SmbConnection | Remove-SmbConnection -Force
     ```

  3. Flush DFS referral cache:

     ```cmd
     dfsutil /pktflush
     ```

  4. Reconnect to `\\LAB.LOCAL\Files\Sales` and confirm new connections and referrals are established.
