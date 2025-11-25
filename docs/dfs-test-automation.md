# DFS Test Automation Guide

One-click lab test runner that handles VM startup, health checks, and test execution.

## Quick Start

```powershell
# Run from repo root as Administrator
.\build\lab-scripts\Run-DfsLabTests.ps1
```

That's it! The script will:

1. Start LAB-DC1, LAB-FS1, LAB-FS2
2. Wait for VMs to be accessible (PowerShell Direct)
3. Wait for AD/DFS services
4. Configure host DNS
5. Run `dotnet test --filter "TestCategory=Lab"`
6. Stop VMs when done

## Usage Examples

```powershell
# Run all lab tests (default)
.\build\lab-scripts\Run-DfsLabTests.ps1

# Run specific tests, keep VMs running after
.\build\lab-scripts\Run-DfsLabTests.ps1 -Filter "NameListReferral" -SkipVmStop

# VMs already running—just run tests
.\build\lab-scripts\Run-DfsLabTests.ps1 -SkipVmStart -SkipVmStop

# Include failover tests (stops/starts FS1 mid-test)
.\build\lab-scripts\Run-DfsLabTests.ps1 -IncludeFailover

# Provide password (otherwise prompts or uses $env:LAB_PASSWORD)
.\build\lab-scripts\Run-DfsLabTests.ps1 -Password "Password123!"
```

## Files

| File | Purpose |
|------|---------|
| `build/lab-scripts/Run-DfsLabTests.ps1` | Main entry point |
| `build/lab-scripts/LabOrchestration.psm1` | VM control functions |

## How It Works

### PowerShell Direct

No network config needed—commands run directly in VMs from Hyper-V host:

```powershell
Invoke-Command -VMName LAB-DC1 -Credential $cred -ScriptBlock { hostname }
```

### Health Check

Before running tests, verifies:

- All 3 VMs accessible via PowerShell Direct
- AD services running (ADWS, DNS)
- DFS service running
- DFS namespace `\\LAB.LOCAL\Files` exists

### Failover Testing

The `-IncludeFailover` flag:

1. Runs normal tests
2. Stops LAB-FS1 (`Stop-VM -Force`)
3. Runs tests tagged `[TestCategory("Failover")]`
4. Restarts LAB-FS1

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `LAB_PASSWORD` | (prompted) | Password for LAB\Administrator |
| `LAB_USERNAME` | Administrator | Username |
| `LAB_DOMAIN` | LAB.LOCAL | Domain name |

## Requirements

- **Run as Administrator** (for Hyper-V and DNS changes)
- **Hyper-V module** installed
- **Lab VMs** configured per `smb-dfs-lab-status.md`

## Troubleshooting

```powershell
# Check VM status
Get-VM LAB-DC1, LAB-FS1, LAB-FS2 | ft Name, State

# Manual health check
Import-Module .\build\lab-scripts\LabOrchestration.psm1
Initialize-LabCredential -Password "Password123!"
Test-LabHealth

# Connect to VM console
vmconnect localhost LAB-DC1
```

See `dfs-e2e-test-plan.md` for detailed test cases.
