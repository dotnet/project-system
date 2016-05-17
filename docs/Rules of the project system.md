#Rules of the project system

The following are a set of rules and guidelines that we should follow as we write the new project system

#### Upgrade
- Existing projects that have been opened in Visual Studio '15' and saved, can be reopened in previous versions of Visual Studio right back to Visual Studio 2010.
- Developers will not be prompted to upgrade, convert or otherwise change their existing projects when opened in Visual Studio '15'

#### Project Files
- New properties and items that are used only for Visual Studio or designer purposes should not be persisted in the project file. This file should be treated as a "user" editable file and as such, should be readable, editable and understandable by the user.
