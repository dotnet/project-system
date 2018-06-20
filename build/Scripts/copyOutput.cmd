echo Copy the Modern Vsixes manifest into VsixV3
robocopy %BinariesDirectory% %BinariesDirectory%VsixV3 Microsoft.VisualStudio.Editors.vsman Microsoft.VisualStudio.ProjectSystem.Managed.vsman /njh /njs /np /xx
