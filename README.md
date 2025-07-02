# Omnitooth - Bluetooth HID Emulator

A modern Windows desktop application that captures local keyboard and mouse input and transmits it wirelessly over Bluetooth to connected client devices. The Windows machine appears as a standard Bluetooth Human Interface Device (HID) using Bluetooth Low Energy (BLE) with HID over GATT Profile (HOGP).

## Features

- **Zero-client software requirement**: Works with any device supporting Bluetooth HID
- **Low-latency input transmission**: Optimized for gaming and professional use
- **Multi-device support**: Connect multiple client devices simultaneously
- **Security-first design**: Encrypted connections with proper authentication
- **Modern UI/UX**: Intuitive WPF interface with real-time status monitoring

## Technology Stack

- **Framework**: .NET 8
- **UI**: WPF with ModernWPF styling
- **Architecture**: Clean Architecture with MVVM pattern
- **Input Capture**: GameInput API (with Raw Input fallback)
- **Bluetooth**: WinRT Bluetooth LE APIs
- **Configuration**: Microsoft.Extensions.Configuration with appsettings.json
- **Logging**: Serilog with structured logging
- **Testing**: xUnit, Moq, FluentAssertions

## Project Structure

```
Omnitooth/
├── src/
│   ├── Omnitooth.Core/           # Domain models & interfaces
│   ├── Omnitooth.Infrastructure/ # Bluetooth, Input, HID implementation
│   ├── Omnitooth.Application/    # Services, Commands, Handlers
│   └── Omnitooth.Presentation/   # WPF UI
├── tests/
│   ├── Omnitooth.Core.Tests/
│   ├── Omnitooth.Infrastructure.Tests/
│   └── Omnitooth.Application.Tests/
├── docs/
└── plan.md                       # Development plan
```

## Requirements

- Windows 11 22H2+ (for modern Windows APIs and GameInput support)
- Bluetooth 4.0+ (required for BLE functionality)
- .NET 8 Runtime
- Administrator Rights (for system-level input capture)

## Development Status

### Phase 1: Foundation & Project Setup ✅
- [x] Solution structure and directories
- [x] .NET 8 projects (Core, Infrastructure, Application, Presentation)
- [x] Central package management (Directory.Packages.props)
- [x] Development tools (EditorConfig, Global.json)
- [x] Dependency injection and configuration infrastructure
- [x] Test projects structure

### Phase 2: Advanced Input Capture System (Planned)
- [ ] GameInput API integration
- [ ] Raw Input fallback implementation
- [ ] Input processing pipeline
- [ ] Performance optimizations

### Phase 3: HID Protocol Implementation (Planned)
- [ ] HID Report descriptors
- [ ] Report builder system
- [ ] Multi-device support

### Phase 4: Bluetooth GATT Server (Planned)
- [ ] GATT service provider
- [ ] HID service characteristics
- [ ] Connection management

### Phase 5: WPF Application (In Progress)
- [x] Basic WPF application structure
- [x] Main window with placeholder functionality
- [ ] MVVM implementation with ViewModels
- [ ] Settings and configuration UI

### Phase 6: Testing & Quality Assurance (Planned)
- [ ] Unit tests
- [ ] Integration tests
- [ ] End-to-end tests

## Building

```bash
dotnet restore
dotnet build
```

## Running

```bash
dotnet run --project src/Omnitooth.Presentation
```

## Configuration

Configuration is managed through `appsettings.json` and `appsettings.Development.json` files. Key configuration sections include:

- **Bluetooth**: Device name, service UUID, connection settings
- **Input**: GameInput preferences, sensitivity, rate limiting
- **HID**: Report rates, batching, compression
- **Security**: Authentication, encryption, device filtering
- **Performance**: Threading, memory management
- **UI**: Theme, startup behavior, notifications

## License

Copyright © 2024 Omnitooth

## Contributing

This project follows clean architecture principles and modern .NET development practices. Please ensure all code follows the established patterns and includes appropriate tests.