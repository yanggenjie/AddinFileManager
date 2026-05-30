# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Revit Add-in Manager - A WPF application for managing Revit plugin add-ins. Enables users to enable/disable/delete Revit add-ins by renaming `.addin` files to `.addin.disable` and vice versa.

## Build Commands

```powershell
# Build solution
dotnet build source\AddinFileManager.slnx

# Build Release
dotnet build source\AddinFileManager.slnx -c Release

# Publish Release (with auto version increment)
.\publish.ps1

# Run application
.\run.ps1
```

## Architecture

- **Target Framework**: .NET Framework 4.7.2 with WPF
- **UI Framework**: MahApps.Metro for modern UI styling
- **MVVM Pattern**: PropertyChanged.Fody for automatic INotifyPropertyChanged implementation

### Project Structure

```
source/AddinFileManager/
├── App.xaml(.cs)           # Application entry point
├── Common/
│   ├── CommonString.cs     # Constants (DisableExt = ".disable")
│   └── ConfigManager.cs    # JSON config for Revit versions
├── UI/
│   ├── Model/
│   │   └── AddinInfoModel.cs    # Add-in data model with enable/disable logic
│   ├── ViewModel/
│   │   └── MainViewModel.cs     # Main window ViewModel
│   └── View/
│       ├── MainWindow.xaml(.cs) # Main window
│       ├── SettingsWindow.xaml(.cs) # Settings dialog
│       └── ConfirmWindow.xaml(.cs)  # Confirmation dialog
├── RelayCommand.cs         # ICommand implementation
└── Properties/
    └── AssemblyInfo.cs     # Version info (manual update required)
```

### Key Components

1. **AddinInfoModel**: Core model that handles `.addin` ↔ `.addin.disable` file renaming. Uses PropertyChanged.Fody's `[OnChangedMethod]` for automatic change notification.

2. **MainViewModel**: Scans Revit add-in directories (both user and global), filters out default Autodesk add-ins, and displays third-party add-ins.

3. **ConfigManager**: Stores user-configurable Revit versions in `%USERPROFILE%\.config\addinFileManager\settings.json`.

### Revit Add-in Directories

- Global: `%ProgramData%\Autodesk\Revit\Addins\{version}`
- User: `%APPDATA%\Autodesk\Revit\Addins\{version}`

### Dependencies

- PropertyChanged.Fody: Auto property change notification
- Costura.Fody: Embeds dependencies as resources (produces single EXE)
- MahApps.Metro: UI controls (ToggleSwitch, styling)
- Newtonsoft.Json: Config file serialization
