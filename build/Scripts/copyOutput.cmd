echo Copy the Modern Vsixes manifest into VsixV3
robocopy %BinariesDirectory% %BinariesDirectory%VsixV3 Microsoft.VisualStudio.Editors.vsman Microsoft.VisualStudio.ProjectSystem.Managed.vsman

echo Copy the Nugets to CoreXT Share
robocopy %BinariesDirectory%NuGetPackages \\cpvsbuild\drops\dd\nuget