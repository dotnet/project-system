# Rules of the Project System

The following are a set of rules and guidelines that we should follow as we write the new project system.

## Upgrade
- Developers will not be prompted to upgrade, convert or otherwise change their existing projects when opened in Visual Studio 2017. 
    
- Existing projects once opened in Visual Studio 2017 and saved, can be reopened in previous versions of Visual Studio right back to Visual Studio 2010

The exception the above rules are XProj-based projects which will be converted to csproj-based projects in Visual Studio 2017.

## Project Files
- New properties and items that are used only for Visual Studio or designer purposes should not be persisted in the project file. This file should be treated as a "user" file and as such, should be readable, easily editable and understandable by the user.

## Visual Studio
- Project System behavioral differences between languages (C#, Visual Basic or F#) or project types (WinForms, Web, etc), such as which files to nest or hide by default, should be configurable and persisted in the associated Microsoft.[Language].Designer.targets file.

