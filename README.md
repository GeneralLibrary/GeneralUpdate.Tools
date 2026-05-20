# GeneralUpdate.Tools

基于 **Avalonia 12 + Semi 主题** 的跨平台桌面打包工具，为 [GeneralUpdate](https://github.com/GeneralLibrary/GeneralUpdate) 生态系统生成增量更新补丁包、扩展包和 OSS 配置。

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.0-orange.svg)](https://avaloniaui.net/)

## ✨ 功能

### 📦 补丁包生成
选择新旧两个版本目录，一键生成 BSDiff40 差分补丁包（`.zip`），直接供 GeneralUpdate 客户端使用。
- 6 个字段，全部手动输入，无自动读取
- 自动识别新增/修改/删除文件
- 产物包含 `*.patch` 差分 + `delete_files.json` 删除清单

### 🧩 扩展包生成
打包扩展项目为 `{Name}_{Version}.zip`，内嵌 `manifest.json` 元信息（名称、版本、发布者、许可证等）。

### ⚙️ OSS 配置生成
管理 OSS 更新清单，计算 SHA256 哈希，批量导出 JSON 配置。

## 🌐 多语言 & 主题

- **中文 / English** 一键切换
- **浅色 / 深色** 主题切换，Semi 控件全面适配

## 🚀 快速开始

### 环境
- .NET 10.0 SDK
- Windows / Linux

### 运行
```bash
cd src
dotnet run
```

### 发布
```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true
```

## 📁 项目结构

```
src/
├── GeneralUpdate.Tools.csproj
├── App.axaml                    Semi v12 主题
├── Models/                      数据模型
├── Services/                    差分 / 压缩 / 哈希
├── ViewModels/                  MVVM 视图模型
├── Views/                       XAML 视图
└── Assets/
```

## 🤝 与 GeneralUpdate 客户端对齐

Tools 产出的 `.zip` 包直接供客户端 Pipeline 消费，无需中间配置桥接。

## 📄 License

Apache 2.0
