# Revit Add-in Manager

一款简洁高效的 Revit 插件管理工具，支持批量启用/禁用、删除插件，并提供多版本 Revit 支持和主题切换功能。

![Version](https://img.shields.io/badge/Version-1.0.7.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Framework](https://img.shields.io/badge/.NET-4.7.2-green)

## 功能特性

### 核心功能
- **插件启用/禁用** - 一键切换 `.addin` ↔ `.addin.disable` 文件
- **批量操作** - 勾选多个插件，批量启用或禁用
- **插件删除** - 安全删除插件文件（含确认对话框）
- **实时搜索** - 按文件名或插件名快速搜索过滤

### 版本管理
- **多版本支持** - 支持 Revit 2016-2027 多个版本
- **版本筛选** - 左侧边栏切换不同 Revit 版本
- **自动过滤** - 自动排除 Autodesk 默认插件（ExportViewSelectorApp、Communicator、FormItConverter、BIM360、Dynamo 等）

### 用户体验
- **主题切换** - 支持浅色/深色主题
- **撤销功能** - 支持撤销上一步操作 (Ctrl+Z)
- **快捷键** - F5 刷新、Ctrl+A 全选、Delete 删除
- **右键菜单** - 查看详情、打开目录、删除插件

### 系统集成
- **安装目录识别** - 自动扫描全局目录 `%ProgramData%\Autodesk\Revit\Addins\{version}` 和用户目录 `%APPDATA%\Autodesk\Revit\Addins\{version}`
- **自动清理** - 当同一插件同时存在 `.addin` 和 `.addin.disable` 时，自动清理 `.disable` 文件
- **权限提示** - 权限不足时提示以管理员身份运行

## 界面预览

```
┌─────────────────────────────────────────────────────────────────┐
│  ⚙️  [主题]                        Revit 插件管理器              │
├──────────┬──────────────────────────────────────────────────────┤
│          │  🔍 搜索插件...            [批量启用] [批量禁用] [↻] [↶] │
│ Revit 2027  │──────────────────────────────────────────────────────│
│   ▪ 3    │  ☐ │  安装位置    │  文件名         │  名称    │  开关  │
│ Revit 2026  │──────────────────────────────────────────────────────│
│   ▪ 12   │  ☑ │  用户目录    │  MyAddin.addin  │  MyAddin │  [●]  │
│ Revit 2025  │  ☐ │  全局目录   │  Tool.addin     │  Tool    │  [○]  │
│   ▪ 8    │  ☑ │  用户目录    │  Helper.addin   │  Helper  │  [●]  │
│          │  ...                                               │
├──────────┴──────────────────────────────────────────────────────┤
│  总计: 23 | 已启用: 15 | 已禁用: 8          最后更新: 2026-06-01   │
└─────────────────────────────────────────────────────────────────┘
```

## 快速开始

### 环境要求
- Windows 10/11
- .NET Framework 4.7.2
- Revit 2016-2027 任一版本

### 构建运行

```powershell
# 构建解决方案
dotnet build source\AddinFileManager.slnx

# 运行应用程序
.\run.ps1

# 发布 Release 版本（含自动版本号递增）
.\publish.ps1
```

### 使用方法

1. 启动应用后，左侧边栏选择要管理的 Revit 版本
2. 在插件列表中勾选需要操作的插件
3. 使用「批量启用」/「批量禁用」按钮，或直接点击开关
4. 支持搜索框输入关键词实时过滤
5. 右键点击插件可查看详情、打开所在目录或删除

## 技术栈

| 技术 | 版本 | 说明 |
|------|------|------|
| .NET Framework | 4.7.2 | 目标框架 |
| WPF | - | UI 框架 |
| MahApps.Metro | 2.4.10 | 现代 UI 样式 |
| PropertyChanged.Fody | 4.1.0 | 自动属性变更通知 |
| Costura.Fody | 5.7.0 | 单文件打包 |
| Newtonsoft.Json | 13.0.4 | 配置序列化 |

## 项目结构

```
source/
├── AddinFileManager.slnx
├── AddinFileManager/
│   ├── App.xaml(.cs)              # 应用程序入口
│   ├── RelayCommand.cs            # ICommand 实现
│   ├── Common/
│   │   ├── CommonString.cs        # 常量定义
│   │   └── ConfigManager.cs       # 配置文件管理
│   ├── Services/
│   │   ├── AddinFileService.cs    # 插件文件操作服务
│   │   ├── DialogService.cs       # 对话框服务
│   │   └── OperationHistoryService.cs # 操作历史服务
│   ├── UI/
│   │   ├── Model/
│   │   │   └── AddinInfoModel.cs  # 插件数据模型
│   │   ├── ViewModel/
│   │   │   └── MainViewModel.cs   # 主窗口 ViewModel
│   │   └── View/
│   │       ├── MainWindow.xaml    # 主窗口
│   │       ├── SettingsWindow.xaml # 设置窗口
│   │       ├── ConfirmWindow.xaml # 确认对话框
│   │       ├── MessageWindow.xaml # 消息对话框
│   │       └── AddinDetailsWindow.xaml # 详情窗口
│   └── Properties/
│       └── AssemblyInfo.cs        # 版本信息
```

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| F5 | 刷新插件列表 |
| Ctrl+Z | 撤销上一步操作 |
| Ctrl+A | 全选/取消全选 |
| Delete | 删除选中的插件 |

## 许可证

MIT License

---

<p align="center">Built with ❤️ for Revit users</p>