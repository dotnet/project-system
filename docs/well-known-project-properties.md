# __Well Known Project Properties, Items and Item Metadata__

There are two types of properties, items and item metadata that are stored in MSBuild project, targets and props files. 

- [Build  Properties, Items andItem  Metadata](#build-properties,-items-and-item-metadata)

- [Designer Properties, Items and Item Metadata](#designer-properties,-items-and-item-metadata)

## __Build Properties, Items and Item Metadata__
These properties, items and item metadata can be used to influence builds.

### __Build Properties__

#### __PreBuildEvent (string)__
| Language      | Default            |
|---------------| -------------------|
| C#            | (empty)            |
| Visual Basic  | (empty)            |

Specifies commands to execute before the build starts.

##### __Example__
``` XML
  <PropertyGroup>
    <PreBuildEvent>call CopyBuildDependencies.cmd</PreBuildEvent>
  </PropertyGroup>
```

#### __PostBuildEvent (string)__
| Language      | Default            |
|---------------| -------------------|
| C#            | (empty)            |
| Visual Basic  | (empty)            |

Specifies commands to excecute after the build ends. To control whether these commands are run on failed or update-to-date builds, set the _RunPostBuildEvent_ property.

##### __Example__
``` XML
  <PropertyGroup>
    <PostBuildEvent>call DeployTestEnvironment.cmd</PostBuildEvent>
  </PropertyGroup>
```

#### __RunPostBuildEvent (enum)__

| Language      | Default            |
|---------------| -------------------|
| C#            | OnBuildSuccess     |
| Visual Basic  | OnBuildSuccess     |

Specifies the conditions for the command in _PostBuildEvent_ to run.

| Value           | Description    |
|-----------------| ---------------|
| Always          | Post-build event will run regardless of whether the build succeeds.     |
| OnOutputUpdated | Post-build event will run if the build succeeds, regardless of whether the project output is up-to-date.|
| OnBuildSuccess  | Post-build event will only when the project output is different than the previous output. |

##### __Example__
``` XML
  <PropertyGroup>
    <RunPostBuildEvent>Always</RunPostBuildEvent>
  </PropertyGroup>
```

## __Designer Properties, Items and Item Metadata__
These properties, items and item metadata are used for solely for Visual Studio and designer purposes, and have no influence on the resulting build.

### __Designer Properties__

#### __AppDesignerFolder (string)__

| Language      | Default            |
|---------------| -------------------|
| C#            | Properties         |
| Visual Basic  | My Project         |

Specifies the name of the _Application Designer_ folder, which by default contains source and other files pertinent to the project as a whole, including AssemblyInfo.cs/AssemblyInfo.vb.

##### __Example__
``` XML
<PropertyGroup>
    <AppDesignerFolder>Dave's Properties</AppDesignerFolder>
<PropertyGroup>
```

#### __AppDesignerFolderContentsVisibleOnlyInShowAllFiles (bool)__

| Language      | Default            |
|---------------| -------------------|
| C#            | false              |
| Visual Basic  | true               |

Specifies whether the contents of the _Application Designer_ folder are only visible when Solution Explorer's _Show All Files_ mode is turned on.

##### __Example__
``` XML
<PropertyGroup>
    <AppDesignerFolderContentsVisibleOnlyInShowAllFiles>true</AppDesignerFolderContentsVisibleOnlyInShowAllFiles>
<PropertyGroup>
```