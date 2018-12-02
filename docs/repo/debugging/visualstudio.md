# Visual Studio

## Figuring out the Git SHA for your current build

1. In Explorer, look at the properties of %VSINSTALLDIR%\Common7\IDE\Extensions\Microsoft\ManagedProjectSystem\Microsoft\Microsoft.VisualStudio.ProjectSystem.Managed.dll

![image](https://user-images.githubusercontent.com/1103906/48829215-dbfe0c80-edc5-11e8-8618-b4c9844359c7.png)

The Product Version field contains the Git SHA from which that branch was built.
