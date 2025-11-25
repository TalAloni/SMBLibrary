# SMB + DFS Lab — Status

> Last updated: 2024-11-24

## Infrastructure

| VM | Role | IP | Domain | Status |
|----|------|-----|--------|--------|
| **LAB-DC1** | Domain Controller + DNS | 10.0.0.10 | LAB.LOCAL | Running ✓ |
| **LAB-FS1** | File Server + DFS | 10.0.0.20 | LAB.LOCAL | Running ✓ |
| **LAB-FS2** | File Server + DFS | 10.0.0.21 | LAB.LOCAL | Running ✓ |

**Network**: Internal vSwitch `LAB_Internal`, host vNIC at `10.0.0.1/24`

**Domain**: `LAB.LOCAL`

**Credentials**: `LAB\Administrator` / `Password123!`

---

## SMB Shares

| Share | Local Path | Status |
|-------|------------|--------|
| `\\LAB-FS1\Sales` | `C:\Data\Sales` | ✓ |
| `\\LAB-FS2\Sales` | `C:\Data\Sales` | ✓ |

---

## DFS Configuration

| Component | Value |
|-----------|-------|
| **Namespace** | `\\LAB.LOCAL\Files` (domain-based, Windows Server 2008 mode) |
| **Folder** | `\\LAB.LOCAL\Files\Sales` |
| **Targets** | `\\LAB-FS1\Sales`, `\\LAB-FS2\Sales` |
| **Replication Group** | `Sales-Replication` |
| **Topology** | Full mesh |
| **Primary Member** | LAB-FS1 |
| **Replication Status** | Working ✓ |

---

## Host Access

To access the lab from the Hyper-V host:

1. Set DNS on the `vEthernet (LAB_Internal)` adapter to `10.0.0.10`
2. Use credentials `LAB\Administrator` / `Password123!`

### Mapped Drives (example)

| Drive | Target | Type |
|-------|--------|------|
| **X:** | `\\10.0.0.20\Sales` | Direct to FS1 |
| **Y:** | `\\LAB.LOCAL\Files\Sales` | DFS namespace |
| **Z:** | `\\10.0.0.21\Sales` | Direct to FS2 |

### Quick Access Commands

```powershell
# Set host DNS
Set-DnsClientServerAddress -InterfaceAlias 'vEthernet (LAB_Internal)' -ServerAddresses '10.0.0.10'

# Map drives with credentials
$cred = Get-Credential -UserName 'LAB\Administrator'
New-PSDrive -Name X -PSProvider FileSystem -Root '\\10.0.0.20\Sales' -Credential $cred -Persist
New-PSDrive -Name Y -PSProvider FileSystem -Root '\\LAB.LOCAL\Files\Sales' -Credential $cred -Persist
New-PSDrive -Name Z -PSProvider FileSystem -Root '\\10.0.0.21\Sales' -Credential $cred -Persist
```

---

## Lab Scripts

| File | Purpose |
|------|---------|
| `build/New-SmbDfsLab.ps1` | Creates Hyper-V VMs from ISO |
| `build/lab-scripts/Configure-LabDC1.ps1` | DC configuration script |
| `build/lab-scripts/Configure-LabFS1.ps1` | FS1 configuration script |
| `build/lab-scripts/Configure-LabFS2.ps1` | FS2 configuration script |
| `build/lab-scripts/Configure-DfsNamespace.ps1` | DFS namespace + replication setup |
| `build/lab-scripts/Deploy-LabConfig.ps1` | Host-side orchestration (PowerShell Direct) |

---

## VM Management

### Start all VMs

```powershell
Start-VM LAB-DC1, LAB-FS1, LAB-FS2
```

### Stop all VMs

```powershell
Stop-VM LAB-DC1, LAB-FS1, LAB-FS2
```

### Check VM status

```powershell
Get-VM LAB-DC1, LAB-FS1, LAB-FS2 | Select-Object Name, State, CPUUsage, MemoryAssigned
```

### Connect to VM via PowerShell Direct

```powershell
$cred = Get-Credential -UserName 'LAB\Administrator'
Enter-PSSession -VMName 'LAB-DC1' -Credential $cred
```

---

## Testing

See [smb-dfs-lab-setup.md](smb-dfs-lab-setup.md) **section 13** for detailed test scenarios:

- **13.1** — SMB connectivity and dialects
- **13.2** — DFS namespace and referrals
- **13.3** — DFS Replication behavior
- **13.4** — Authentication and authorization
- **13.5** — Client caching (optional)
- **13.6** — Troubleshooting commands

### Quick Failover Test

```powershell
# Check current DFS target
dfsutil /pktinfo

# Stop FS1 and verify failover
Stop-VM LAB-FS1 -Force
dir Y:\   # Should still work via FS2

# Restore FS1
Start-VM LAB-FS1
```

### Quick Replication Test

```powershell
# Create file on FS1
"Test $(Get-Date)" | Out-File X:\replication-test.txt

# Wait and check FS2
Start-Sleep -Seconds 30
Get-Content Z:\replication-test.txt
```
