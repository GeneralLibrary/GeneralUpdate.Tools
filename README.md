# 🛠️ GeneralUpdate.Tools

<div align="center">

![GeneralUpdate Logo](imgs/GeneralUpdate_h.png)

**A powerful desktop tool for managing software updates and extensions** 🚀

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Avalonia](https://img.shields.io/badge/Avalonia-11.2-orange.svg)](https://avaloniaui.net/)

</div>

---

## 📖 What is GeneralUpdate.Tools?

**GeneralUpdate.Tools** is a user-friendly desktop application that helps developers create and manage software update packages. It's part of the [GeneralUpdate](https://github.com/GeneralLibrary/GeneralUpdate) ecosystem - a complete solution for implementing automatic updates in your applications.

Think of it as your **"Update Package Workshop"** 🏭 where you can:
- 📦 Create differential update packages (only ship what changed!)
- 📝 Generate version configuration files
- 🧩 Package and manage application extensions

### ✨ Why Use This Tool?

- **💡 Beginner-Friendly**: Simple visual interface - no complex command lines
- **⚡ Save Bandwidth**: Create differential packages that only include changed files
- **🎯 Cross-Platform**: Works on Windows and Linux
- **🔧 All-in-One**: Three powerful tools in one application

---

## 🎯 Core Features

### 1️⃣ Packet Builder 📦

Create differential update packages for your applications. Instead of shipping the entire application again, only include the files that changed!

**What it does:**
- Compares your old version with the new version
- Identifies only the changed files
- Creates a compressed update package (.zip)
- Optionally includes driver files

**Perfect for:** Reducing download sizes and update times

---

### 2️⃣ OSS Packet Builder 📝

Generate `version.json` configuration files that tell your application where and how to download updates.

**What it does:**
- Creates version configuration in JSON format
- Specifies update package URLs and metadata
- Manages version history
- Supports hash verification for security

**Perfect for:** Managing update metadata and version tracking

---

### 3️⃣ Extension Manager 🧩

Package your application extensions into distributable packages with proper metadata.

**What it does:**
- Compresses extension directories into .zip files
- Generates manifest.json with extension metadata
- Supports custom properties and dependencies
- Platform-specific targeting (Windows/Linux/MacOS)

**Perfect for:** Creating plugin systems and extension marketplaces

---

## 🚀 Quick Start Guide

### Prerequisites

Before you begin, make sure you have:

- ✅ **Operating System**: Windows 10+ or Linux (Ubuntu 20.04+)
- ✅ **.NET Runtime**: [.NET 8.0 SDK or Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ **Disk Space**: At least 200MB free space

### 📥 Installation

#### Option 1: Download Pre-built Release (Recommended for Beginners)

1. Go to the [Releases](https://github.com/GeneralLibrary/GeneralUpdate.Tools/releases) page
2. Download the latest version for your platform
3. Extract the ZIP file to a folder
4. Run the executable:
   - **Windows**: Double-click `GeneralUpdate.Tool.Avalonia.exe`
   - **Linux**: Run `./GeneralUpdate.Tool.Avalonia` in terminal

#### Option 2: Build from Source

```bash
# 1. Clone the repository
git clone https://github.com/GeneralLibrary/GeneralUpdate.Tools.git
cd GeneralUpdate.Tools

# 2. Navigate to the source folder
cd src

# 3. Restore dependencies
dotnet restore

# 4. Build the project
dotnet build

# 5. Run the application
dotnet run
```

---

## 📚 Usage Tutorial (Cookbook)

### 🍳 Recipe 1: Creating Your First Update Package

Let's create a differential update package step by step!

**Scenario:** You have version 1.0.0 of your app and want to update it to version 2.0.0

#### Step 1: Prepare Your Directories

You need three folders:
- **App Directory** (📂 Old Version): Contains your version 1.0.0 files
- **Release Directory** (📂 New Version): Contains your version 2.0.0 files  
- **Patch Directory** (📂 Output): Where the differential files will be saved

```
Example structure:
C:/MyApp/v1.0.0/     ← Old version
C:/MyApp/v2.0.0/     ← New version
C:/MyApp/patch/      ← Temporary output folder
```

#### Step 2: Open Packet Builder

1. Launch GeneralUpdate.Tools
2. Click on the **"Packet"** tab at the top
3. You'll see a form with several fields

#### Step 3: Configure Your Package

Fill in the fields:

| Field | What to Enter | Example |
|-------|---------------|---------|
| **Name** | Package filename | `MyApp_Update_v2.0.0` |
| **App Directory** | Path to old version | `C:/MyApp/v1.0.0/` |
| **Release Directory** | Path to new version | `C:/MyApp/v2.0.0/` |
| **Patch Directory** | Temp output path | `C:/MyApp/patch/` |
| **Format** | Compression format | `.zip` (default) |
| **Encoding** | Text encoding | `UTF-8` (recommended) |

#### Step 4: Optional - Add Driver Files

If your update includes driver files:
1. Click **"Select"** next to "Driver Directory"
2. Choose the folder containing your driver files
3. They'll be automatically placed in a `drivers/` folder inside the package

#### Step 5: Build the Package!

1. Click the **"Build"** button 🔨
2. Wait for processing (may take a few moments)
3. Success! 🎉 You'll see a message "Build success"
4. Your update package is saved in the parent directory of your Patch Directory

**Result:** You now have a `.zip` file containing only the changed files!

---

### 🍳 Recipe 2: Creating Version Configuration (OSS Packet)

Now let's create a `version.json` file that tells your application about available updates.

#### Step 1: Open OSS Packet Builder

1. Click on the **"OSS Packet"** tab
2. You'll see fields for version information

#### Step 2: Enter Version Information

Fill in the details for your update:

| Field | What to Enter | Example |
|-------|---------------|---------|
| **Version** | Version number | `2.0.0.0` |
| **Packet Name** | Name of your .zip file | `MyApp_Update_v2.0.0.zip` |
| **URL** | Download URL | `https://updates.myapp.com/packages/MyApp_Update_v2.0.0.zip` |
| **Date** | Release date | `2026-02-11` |
| **Time** | Release time | `16:30:00` |

#### Step 3: Calculate Hash (Security)

1. Click the **"Hash"** button 🔒
2. Select your `.zip` package file
3. The tool will calculate a SHA256 hash
4. This ensures the download wasn't corrupted or tampered with

#### Step 4: Add to Configuration

1. Click **"Append"** ➕ to add this version to the list
2. You can add multiple versions (version history)
3. The JSON preview will update automatically

#### Step 5: Save Configuration

1. Click **"Build"** button 💾
2. Choose where to save your `version.json` file
3. Success! You can now upload this file to your server

**Result:** You have a `version.json` file that looks like this:

```json
[
  {
    "Version": "2.0.0.0",
    "PacketName": "MyApp_Update_v2.0.0.zip",
    "Url": "https://updates.myapp.com/packages/MyApp_Update_v2.0.0.zip",
    "Hash": "abc123def456...",
    "Date": "2026-02-11T16:30:00"
  }
]
```

---

### 🍳 Recipe 3: Packaging an Extension

Create a distributable extension package with metadata.

#### Step 1: Prepare Your Extension

Make sure you have:
- A folder containing your extension files
- Extension metadata (name, version, description)

#### Step 2: Open Extension Manager

1. Click on the **"Extension"** tab
2. You'll see a comprehensive form

#### Step 3: Fill Basic Information

| Field | What to Enter | Example |
|-------|---------------|---------|
| **Name** | Extension identifier | `MyAwesomePlugin` |
| **Display Name** | User-friendly name | `My Awesome Plugin` |
| **Version** | Version number | `1.0.0.0` |
| **Description** | What it does | `Adds awesome features to your app` |
| **Publisher** | Your name/company | `Your Company Name` |

#### Step 4: Select Directories

1. **Extension Directory**: Click "Select" and choose your extension folder
2. **Export Path**: Click "Select" and choose where to save the package

#### Step 5: Configure Platform & Details

- **Platform**: Select target platform (Windows/Linux/MacOS/All)
- **License**: Enter license type (e.g., "MIT", "Apache-2.0")
- **Categories**: Enter categories separated by commas (e.g., "Tools, Productivity")

#### Step 6: Advanced Options (Optional)

- **Dependencies**: Enter required extension IDs (comma-separated)
- **Min/Max Host Version**: Specify compatible app versions
- **Custom Properties**: Add key-value pairs for extra metadata

#### Step 7: Generate Package

1. Click **"Generate"** button 🎁
2. Wait for compression
3. Success! Your extension package is created

**Result:** You get a `.zip` file like `MyAwesomePlugin_1.0.0.0.zip` containing:
- All your extension files
- A `manifest.json` with all the metadata

---

## 🎨 Understanding the Interface

### Main Window Layout

```
┌─────────────────────────────────────┐
│  [Packet] [OSS Packet] [Extension]  │  ← Tabs
├─────────────────────────────────────┤
│                                     │
│         Feature Content Area        │
│                                     │
│  (Forms and controls for selected   │
│   feature appear here)              │
│                                     │
└─────────────────────────────────────┘
```

### Common Buttons

- 🔨 **Build/Generate**: Creates the package/file
- 📁 **Select**: Opens folder picker
- ➕ **Append**: Adds item to list
- 🗑️ **Clear**: Resets form fields
- 📋 **Copy**: Copies content to clipboard
- 🔒 **Hash**: Calculates file hash

---

## ❓ Frequently Asked Questions (FAQ)

### Q: What's the difference between "Packet" and "OSS Packet"?

**A:** Think of it this way:
- **Packet Builder** creates the actual update files (the .zip package)
- **OSS Packet Builder** creates the configuration that tells where to download it

You need both! First create the package, then create the configuration file.

### Q: Do I need to include all files in the update package?

**A:** No! That's the beauty of differential updates. The tool automatically:
1. Compares old vs. new versions
2. Only includes changed/new files
3. Removes deleted files during update

This saves bandwidth and speeds up downloads! 🚀

### Q: What is the Hash field for?

**A:** The hash is like a fingerprint for your file. It ensures:
- ✅ File wasn't corrupted during download
- ✅ File wasn't tampered with
- ✅ Users get exactly what you published

Always calculate and include the hash for security!

### Q: Can I use this for commercial projects?

**A:** Yes! The project uses Apache 2.0 license, which allows commercial use. See [LICENSE](LICENSE) for details.

### Q: What platforms are supported?

**A:** The tool runs on:
- ✅ Windows 10 and later
- ✅ Linux (Ubuntu, Debian, Fedora, etc.)

Generated packages can target:
- ✅ Windows
- ✅ Linux  
- ✅ MacOS (via Extension Manager)

### Q: Where can I learn more about GeneralUpdate?

**A:** Check out the main project:
- 📖 [GeneralUpdate Documentation](https://github.com/GeneralLibrary/GeneralUpdate)
- 💬 [Community Discussions](https://github.com/GeneralLibrary/GeneralUpdate/discussions)

---

## 🐛 Troubleshooting

### Problem: "Build fail" message appears

**Solutions:**
1. ✅ Check that all directory paths exist
2. ✅ Ensure you have write permissions
3. ✅ Make sure disk has enough free space
4. ✅ Verify .NET 8.0 is properly installed

### Problem: "Access denied" error

**Solutions:**
1. ✅ Run the application as administrator (Windows)
2. ✅ Check folder permissions
3. ✅ Make sure files aren't locked by another program

### Problem: Application won't start

**Solutions:**
1. ✅ Install [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
2. ✅ Check system requirements
3. ✅ Try extracting to a different folder
4. ✅ Disable antivirus temporarily (some may block it)

### Problem: Hash calculation takes too long

**Solutions:**
1. ✅ Normal for large files (>500MB)
2. ✅ Be patient - it ensures security!
3. ✅ Consider splitting very large packages

---

## 🤝 Contributing

We welcome contributions! Whether you:
- 🐛 Found a bug
- 💡 Have a feature idea
- 📝 Want to improve documentation
- 🔧 Want to submit code

Please:
1. Check [existing issues](https://github.com/GeneralLibrary/GeneralUpdate.Tools/issues)
2. Open a new issue to discuss
3. Fork the repository
4. Submit a pull request

---

## 📄 License

This project is licensed under the **Apache License 2.0** - see the [LICENSE](LICENSE) file for details.

You are free to:
- ✅ Use commercially
- ✅ Modify
- ✅ Distribute
- ✅ Sublicense

---

## 🌟 Related Projects

- **[GeneralUpdate](https://github.com/GeneralLibrary/GeneralUpdate)** - The main update framework
- **[GeneralUpdate.Core](https://www.nuget.org/packages/GeneralUpdate.Core/)** - Core update library

---

## 📞 Support & Community

Need help? Have questions?

- 💬 [GitHub Discussions](https://github.com/GeneralLibrary/GeneralUpdate.Tools/discussions)
- 🐛 [Report Issues](https://github.com/GeneralLibrary/GeneralUpdate.Tools/issues)
- ⭐ Star this repo if you find it useful!

---

<div align="center">

**Made with ❤️ by the GeneralUpdate Team**

[⬆ Back to Top](#️-generalupdatetools)

</div>
