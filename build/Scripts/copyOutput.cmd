@if not defined _echo @echo off
setlocal enabledelayedexpansion

set BinariesDirectory=%1

echo Copy the Modern Vsixes manifest into VsixV3
robocopy %BinariesDirectory% %BinariesDirectory%VsixV3 Microsoft.VisualStudio.Editors.vsman Microsoft.VisualStudio.ProjectSystem.Managed.vsman Microsoft.VisualStudio.NetCore.ProjectTemplates.vsman Microsoft.VisualStudio.NetCore.ProjectTemplates.1.x.vsman /njh /njs /np /xx