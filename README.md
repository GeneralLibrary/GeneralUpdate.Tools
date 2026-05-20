# GeneralUpdate.Tools

Cross-platform desktop packaging tool built with **Avalonia 12 + Semi Theme** for the [GeneralUpdate](https://github.com/GeneralLibrary/GeneralUpdate) ecosystem. Generates incremental update patches, extension packages, and OSS configurations.

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.0-orange.svg)](https://avaloniaui.net/)

## Features

### 📦 Patch Package
Select two directories (old vs new), generate a BSDiff40 differential patch package (`.zip`) with one click. Ready for direct consumption by the GeneralUpdate client.
- 6 manual-input fields, no auto-detection
- Auto-identifies added / modified / deleted files
- Output includes `*.patch` files + `delete_files.json`

### 🧩 Extension Package
Package extension projects as `{Name}_{Version}.zip` with embedded `manifest.json` metadata (name, version, publisher, license, etc.).

### ⚙️ OSS Config
Manage OSS update manifests — compute SHA256 hashes, batch export JSON configurations.

## i18n & Theme

- **中文 / English** toggle in sidebar
- **Light / Dark** theme with full Semi control library support

## Quick Start

### Prerequisites
- .NET 10.0 SDK
- Windows or Linux

### Run
```bash
cd src
dotnet run
```

### Publish
```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true
```

## Project Structure

```
src/
├── GeneralUpdate.Tools.csproj
├── App.axaml                    Semi v12 theme
├── Models/                      Data models
├── Services/                    Diff / Package / Hash
├── ViewModels/                  MVVM view models
├── Views/                       XAML views
└── Assets/
```

## Alignment with GeneralUpdate Client

Tools output `.zip` packages are consumed directly by the client Pipeline — no intermediate configuration bridge needed.

## License

Apache 2.0
