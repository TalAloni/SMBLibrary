# DFS Implementation Analysis: smbprotocol (Python)

> **Repository**: https://github.com/jborean93/smbprotocol  
> **Language**: Python 3  
> **License**: MIT  
> **Analysis Date**: 2024-11-24

---

## Summary

**smbprotocol** provides a **complete DFS client implementation** in Python with clean, well-documented code. It implements referral caching, domain caching, V1-V4 referral parsing including NameListReferral support, and integrates DFS resolution into the connection flow. The implementation is particularly notable for its clear structure and comprehensive docstrings.

---

## DFS Support Level: ✅ Full

| Feature | Status | Notes |
|---------|--------|-------|
| DFS Referral Request | ✅ | `FSCTL_DFS_GET_REFERRALS` |
| DFS Referral Request EX | ✅ | `FSCTL_DFS_GET_REFERRALS_EX` with site name |
| V1 Referrals | ✅ | `DFSReferralEntryV1` |
| V2 Referrals | ✅ | `DFSReferralEntryV2` |
| V3/V4 Referrals | ✅ | `DFSReferralEntryV3` (handles both) |
| Referral Cache | ✅ | List-based with prefix matching |
| Domain Cache | ✅ | `DomainEntry` class |
| Target Failover | ✅ | Iterator-based target cycling |
| NameListReferral | ✅ | DC discovery support |
| MaxReferralLevel | ✅ | Default = 4 |

---

## File Structure

```text
src/smbprotocol/
└── dfs.py              # All DFS implementation in single file (~460 lines)
```

---

## Key Implementation Details

### 1. DFSTarget Named Tuple

Simple immutable structure for target entries:

```python
DFSTarget = namedtuple("DFSTarget", ["target_path", "set_boundary"])
"""
Attributes:
    target_path (str): The NetworkPath value in the referral entry.
    set_boundary (bool): Whether TARGET_SET_BOUNDARY flag was set.
"""
```

### 2. DomainEntry (Domain Cache Entry)

```python
class DomainEntry:
    """A DomainCache entry for domain and DC referral requests."""
    
    def __init__(self, referral: DFSReferralEntryV3):
        self.domain_list = []           # List of DC hostnames
        self._referral = referral
        self._start_time: float = time.time()
        self._domain_hint_idx: int | None = None
    
    @property
    def domain_name(self) -> str:
        return self._referral.dfs_path
    
    @property
    def dc_hint(self) -> str:
        """The last known good domain hostname."""
        return self.domain_list[self._domain_hint_idx]
    
    @property
    def is_expired(self) -> bool:
        return ((time.time() - self._start_time) - 
                self._referral["time_to_live"].get_value()) >= 0
    
    @property
    def is_valid(self) -> bool:
        return self._domain_hint_idx is not None and not self.is_expired
    
    def process_dc_referral(self, referral: DFSReferralResponse) -> None:
        """Process DC referral response and populate domain_list."""
        if self._domain_hint_idx is None:
            self._domain_hint_idx = 0
        for dc_entry in referral["referral_entries"].get_value():
            for dc_hostname in dc_entry.network_address:
                if dc_hostname not in self.domain_list:
                    self.domain_list.append(dc_hostname)
```

### 3. ReferralEntry (Referral Cache Entry)

```python
class ReferralEntry:
    """A ReferralCache entry - parsed referral response for caching."""
    
    def __init__(self, referral: DFSReferralResponse):
        self._referral_header_flags = referral["referral_header_flags"]
        self._referrals = referral["referral_entries"].get_value()
        self._start_time: float = time.time()
        self._target_hint_idx: int = 0
    
    @property
    def dfs_path(self) -> str:
        return self._referrals[self._target_hint_idx].dfs_path
    
    @property
    def is_root(self) -> bool:
        return self._referrals[self._target_hint_idx]["server_type"].has_flag(
            DFSServerTypes.ROOT_TARGETS)
    
    @property
    def is_link(self) -> bool:
        return not self.is_root
    
    @property
    def is_expired(self) -> bool:
        referral = self._referrals[self._target_hint_idx]
        return ((time.time() - self._start_time) - 
                referral["time_to_live"].get_value()) >= 0
    
    @property
    def target_failback(self) -> bool:
        return self._referral_header_flags.has_flag(
            DFSReferralHeaderFlags.TARGET_FAIL_BACK)
    
    @property
    def target_hint(self) -> DFSTarget:
        return self.target_list[self._target_hint_idx]
    
    @property
    def target_list(self) -> list[DFSTarget]:
        return [
            DFSTarget(
                target_path=e.network_address,
                set_boundary=e["referral_entry_flags"].has_flag(
                    DFSReferralEntryFlags.TARGET_SET_BOUNDARY),
            )
            for e in self._referrals
        ]
    
    def __iter__(self) -> Iterator[DFSTarget]:
        """Iterates through targets, starting with hint."""
        yield self.target_list[self._target_hint_idx]
        for idx, target in enumerate(self.target_list):
            if idx == self._target_hint_idx:
                continue
            yield target
```

### 4. Flag Definitions

```python
class DFSReferralRequestFlags:
    """[MS-DFSC] 2.2.3 REQ_GET_DFS_REFERRAL_EX RequestFlags"""
    SITE_NAME = 0x00000001

class DFSReferralHeaderFlags:
    """[MS-DFSC] 2.2.4 RESP_GET_DFS_REFERRAL ReferralHeaderFlags"""
    REFERRAL_SERVERS = 0x00000001
    STORAGE_SERVERS = 0x00000002
    TARGET_FAIL_BACK = 0x00000004

class DFSServerTypes:
    """[MS-DFSC] 2.2.5.1 DFS_REFERRAL_V1 ServerType"""
    NON_ROOT_TARGETS = 0x0000
    ROOT_TARGETS = 0x0001

class DFSReferralEntryFlags:
    """[MS-DFSC] 2.2.5.3/2.2.5.4 ReferralEntryFlags"""
    NAME_LIST_REFERRAL = 0x0002
    TARGET_SET_BOUNDARY = 0x0004
```

### 5. DFSReferralRequest

```python
class DFSReferralRequest(Structure):
    """[MS-DFSC] 2.2.2 REQ_GET_DFS_REFERRAL"""
    
    def __init__(self):
        self.fields = OrderedDict([
            ("max_referral_level", IntField(size=2, default=4)),  # Request V4!
            ("request_file_name", TextField(null_terminated=True)),
        ])
```

### 6. DFSReferralRequestEx (with Site Name)

```python
class DFSReferralRequestEx(Structure):
    """[MS-DFSC] 2.2.3 REQ_GET_DFS_REFERRAL_EX"""
    
    def __init__(self):
        self.fields = OrderedDict([
            ("max_referral_level", IntField(size=2, default=4)),
            ("request_flags", FlagField(size=2, flag_type=DFSReferralRequestFlags)),
            ("request_data_length", IntField(size=4, default=lambda s: ...)),
            ("request_file_name_length", IntField(size=2, ...)),
            ("request_file_name", TextField(null_terminated=True, ...)),
            ("site_name_length", IntField(size=2, ...)),
            ("site_name", TextField(null_terminated=True, ...)),
        ])
```

### 7. V3/V4 Referral with NameListReferral Support

```python
class DFSReferralEntryV3(Structure):
    """[MS-DFSC] 2.2.5.3 DFS_REFERRAL_V3 (also handles V4)"""
    
    def process_string_buffer(self, buffer, entry_offset):
        is_name_list = self["referral_entry_flags"].has_flag(
            DFSReferralEntryFlags.NAME_LIST_REFERRAL)
        
        buffer_fields = ["dfs_path", "network_address"]
        if not is_name_list:
            buffer_fields.insert(1, "dfs_alternate_path")
        
        for field_name in buffer_fields:
            field_offset = self[f"{field_name}_offset"].get_value()
            if field_offset == 0:
                continue
            
            string_offset = field_offset - entry_offset
            
            if is_name_list and field_name == "network_address":
                # NameListReferral: network_address is a list of DC names
                value = []
                for _ in range(self["dfs_alternate_path_offset"].get_value()):
                    field = TextField(null_terminated=True, encoding="utf-16-le")
                    field.unpack(buffer[string_offset:])
                    value.append(field.get_value())
                    string_offset += len(field)
            else:
                field = TextField(null_terminated=True, encoding="utf-16-le")
                field.unpack(buffer[string_offset:])
                value = field.get_value()
            
            setattr(self, field_name, value)
```

### 8. Response Parsing with Version Detection

```python
class DFSReferralResponse(Structure):
    def _create_dfs_referral_entry(self, data):
        results = []
        for _ in range(self.fields["number_of_referrals"].get_value()):
            b_version = data[:1]
            if b_version == b"\x01":
                referral_entry = DFSReferralEntryV1()
            elif b_version == b"\x02":
                referral_entry = DFSReferralEntryV2()
            else:
                referral_entry = DFSReferralEntryV3()  # Handles V3 and V4
            
            data = referral_entry.unpack(data)
            results.append(referral_entry)
        
        # Process string buffers in reverse order
        entry_offset = 0
        for referral_entry in reversed(results):
            entry_offset += referral_entry["size"].get_value()
            referral_entry.process_string_buffer(data, entry_offset)
        
        return results
```

---

## Integration Patterns (from smbprotocol main code)

### Referral Request Helper

```python
def dfs_request(tree: TreeConnect, path: str) -> DFSReferralResponse:
    dfs_referral = DFSReferralRequest()
    dfs_referral["request_file_name"] = path
    
    ioctl_req = SMB2IOCTLRequest()
    ioctl_req["ctl_code"] = CtlCode.FSCTL_DFS_GET_REFERRALS
    ioctl_req["file_id"] = b"\xff" * 16  # Reserved file ID for DFS
    ioctl_req["max_output_response"] = 56 * 1024
    ioctl_req["flags"] = IOCTLFlags.SMB2_0_IOCTL_IS_FSCTL
    ioctl_req["buffer"] = dfs_referral
    
    # Send and receive...
    return dfs_response
```

### Path Resolution with Caching

```python
def get_smb_tree(path, ...):
    path_split = [p for p in path.split("\\") if p]
    
    # 1. Check referral cache first
    referral = client_config.lookup_referral(path_split)
    if referral and not referral.is_expired:
        path = path.replace(referral.dfs_path, 
                           referral.target_hint.target_path, 1)
        return connect_to_resolved_path(path)
    
    # 2. Check domain cache
    domain = client_config.lookup_domain(path_split[0])
    if domain:
        # Get DC referral, then root referral
        ...
    
    # 3. Try direct connect, handle BadNetworkName with DFS
    try:
        tree.connect()
    except BadNetworkName:
        # Issue DFS referral request and retry
        ...
```

### STATUS_PATH_NOT_COVERED Handling

```python
try:
    tree.connect()
except BadNetworkName:
    if session.connection.server_capabilities.has_flag(SMB2_GLOBAL_CAP_DFS):
        ipc_tree = get_smb_tree(f"\\\\{server}\\IPC$")
        referral = dfs_request(ipc_tree, path)
        # Retry with resolved path
```

---

## Patterns to Adopt for SMBLibrary

1. **Clean separation** - All DFS code in single module
2. **Named tuples** for immutable target entries
3. **Property-based access** for cache entry attributes
4. **Iterator pattern** for target failover (`__iter__`)
5. **NameListReferral detection** via flag check
6. **Default MaxReferralLevel = 4** for V4 support
7. **TTL tracking** with `time.time()` comparison
8. **Site name support** in extended request

---

## References

- Source: `c:\dev\smbprotocol-reference\src\smbprotocol\dfs.py`
- MS-DFSC specification references in docstrings
