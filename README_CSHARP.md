# Phira MP Server - C# Implementation

This is a fully compatible C# implementation of the Phira Multiplayer server. It implements the exact same binary protocol and behavior as the original Rust server, allowing original Phira clients to connect seamlessly.

## Features

✅ **Protocol Compatible** - 100% compatible with original phira-mp protocol
- Binary serialization with little-endian encoding
- ULEB128 variable-length integer encoding
- TCP framing with exact packet format
- Version negotiation
- Heartbeat/ping-pong mechanism

✅ **Complete Functionality**
- User authentication via Phira API
- Room creation and management
- Chart selection and synchronization
- Game state management (SelectChart → WaitForReady → Playing)
- Touch and judge event broadcasting to monitors
- Host cycling and room locking
- Reconnection handling
- Dangling connection cleanup

## Requirements

- .NET 8.0 or later
- Windows, Linux, or macOS

## Building

```bash
dotnet build
```

## Running

### Basic usage
```bash
dotnet run
```

### Custom port
```bash
dotnet run -- --port 8080
```

### With logging
```bash
# On Linux/macOS
DOTNET_ENVIRONMENT=Development dotnet run

# On Windows (PowerShell)
$env:DOTNET_ENVIRONMENT="Development"; dotnet run
```

## Configuration

Create a `server_config.yml` file in the same directory as the executable:

```yaml
monitors:
  - 2
  - 123
  - 456
```

This configures which user IDs are allowed to join rooms as monitors.

## Protocol Details

### Connection Flow
1. Client connects via TCP
2. Server sends protocol version byte (0x00)
3. Client sends `Authenticate` command with token
4. Server validates token with Phira API
5. Server responds with user info and room state (if reconnecting)
6. Client can now send commands

### Binary Format

**Packet Format:**
```
[ULEB128 length][payload bytes]
```

**ULEB128 Encoding:**
- 7 bits of data per byte
- MSB (bit 7) = continuation bit
- Little-endian order

**Data Types:**
- `u8/i8`: 1 byte
- `u16/i16`: 2 bytes, little-endian
- `u32/i32`: 4 bytes, little-endian
- `u64/i64`: 8 bytes, little-endian
- `f32`: 4 bytes, IEEE 754 little-endian
- `bool`: 1 byte (0 or 1)
- `string`: ULEB128 length + UTF-8 bytes
- `Vec<T>`: ULEB128 count + elements
- `Option<T>`: bool (has_value) + value if true
- `Result<T, E>`: bool (is_ok) + value or error

### Commands

**Client → Server:**
- Ping
- Authenticate
- Chat
- Touches / Judges
- CreateRoom / JoinRoom / LeaveRoom
- LockRoom / CycleRoom
- SelectChart
- RequestStart / Ready / CancelReady
- Played / Abort

**Server → Client:**
- Pong
- Authenticate (response)
- Message (chat, game events)
- Touches / Judges (for monitors)
- ChangeState / ChangeHost
- OnJoinRoom
- Command responses (success/error)

## Architecture

```
PhiraMpServer/
├── Common/
│   ├── BinaryData.cs        # Binary serialization
│   ├── Commands.cs          # Protocol commands
│   └── Stream.cs            # TCP stream with framing
├── Server/
│   ├── Server.cs            # Main server class
│   ├── Session.cs           # Client session & authentication
│   └── Room.cs              # Room state management
└── Program.cs               # Entry point
```

### Key Classes

**BinaryReader/BinaryWriter**: Compatible with Rust's binary format
- Little-endian encoding
- ULEB128 variable-length integers
- Exact same byte layout as Rust implementation

**ClientStream**: Handles TCP connection with framing
- Automatic packet framing (ULEB128 length prefix)
- Separate send/receive tasks
- Heartbeat monitoring

**Session**: Manages client connection
- Authentication via Phira API
- Command processing
- User state management

**Room**: Game room management
- User list (players + monitors)
- State machine (SelectChart → WaitForReady → Playing)
- Chart selection and game flow
- Host management and cycling

**User**: Represents connected user
- Session reference (weak)
- Room membership
- Monitor status
- Reconnection and dangling handling

## Differences from Rust Version

While functionally identical, this C# implementation:
- Uses async/await instead of Tokio
- Uses `ConcurrentDictionary` instead of `Arc<RwLock<HashMap>>`
- Uses `Channel<T>` instead of `mpsc::channel`
- Uses .NET's `HttpClient` for Phira API calls
- Uses YamlDotNet for config parsing

All protocol behavior and binary formats are **exactly the same**.

## Testing with Original Client

This server is designed to work with the original Phira game client without any modifications:

1. Start the C# server:
   ```bash
   dotnet run -- --port 12346
   ```

2. Configure Phira client to connect to your server IP and port

3. The client will work exactly as it does with the Rust server

## Compatibility

✅ Compatible with original phira-mp-client
✅ Same protocol version (0)
✅ Same binary encoding
✅ Same packet format
✅ Same API endpoints
✅ Same behavior

## License

This implementation follows the same license as the original phira-mp project.
