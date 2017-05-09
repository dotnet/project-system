# What determines when a project is opened with the new project system?

When opening a solution, there are guids stored in the .sln file that tell VS to find a project system registered with that Guid. For example, if the sln file has an entry like 

```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ClassLibrary3", "ClassLibrary3\ClassLibrary3.csproj", "{5B9CCABF-2F7E-407C-ACD7-53C37BB859EE}"
EndProject
```

VS first looks for a type called IVsProjectSelector that's registered with that guid. If there is one, it asks the selector to give it the guid of the project system that should be used to open the project.
If there is no selector, then that guid is the one that's used.

In the case above, `FAE04EC0-301F-11D3-BF4B-00C04F79EFBC` is the guid of the old project system. However, there is also a IVsProjectSelector registered for that guid. That project selector sniffs the project to look for a `TargetFramework` or `TargetFrameworks` property and if one exists returns the guid of the new project system. Otherwise it falls back to the old project system. See https://github.com/dotnet/project-system/issues/1358 for some limitations of the selector today.

**NOTE: Looking at the properties mentioned above is an implementation detail of the selector and is bound to change as more project types are supported by CPS (eventually getting rid of the need for a selector). Please don't take a dependency on this behavior as it will break.**

Consider a different solution that looks like this:

```
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "ClassLibrary3", "ClassLibrary3\ClassLibrary3.csproj", "{5B9CCABF-2F7E-407C-ACD7-53C37BB859EE}"
EndProject
```

In this case, the guid `9A19103F-16F7-4668-BE54-9A1E7A4F7556` is the guid for the new project system and there is no selector registered to that guid. So VS will directly open the project with the new project system. This is an easy way to force VS to use the new project system for a project.

# List of GUIDs

Project System | Guid | Has selector? | Current selector behavior
---|---|---|---
Old C# PS | FAE04EC0-301F-11D3-BF4B-00C04F79EFBC | Yes | Selects new PS if project has TargetFramework\TargetFrameworks property. Otherwise fall back to old PS.
New C# PS | 9A19103F-16F7-4668-BE54-9A1E7A4F7556 | No  | 
Old VB PS | F184B08F-C81C-45F6-A57F-5ABD9991F28F | Yes | Selects new PS if project has TargetFramework\TargetFrameworks property. Otherwise fall back to old PS.
New VB PS | 778DAE3C-4631-46EA-AA77-85C1314464D9 | No  | 
Old F# PS | F2A71F9B-5D33-465A-A702-920D77279786 | Yes | Selects new PS if project has the SDK attribute like `<Project Sdk="">` or `<Import Sdk=""`. Otherwise fall back to old PS.
New F# PS | 6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705 | No  | 
