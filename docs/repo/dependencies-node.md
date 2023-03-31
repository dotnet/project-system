# Dependencies node

This document, along with all other docs in this `docs/repo` folder, are aimed at developers of the `dotnet/project-system` repo, or extenders of the project system for other project types.

- General documentation on the dependencies tree features and diagnosing common problems is available in [Dependencies tree](../dependencies-tree.md).
- A high-level overview of the implementation of the dependencies tree is available in [Dependencies Node Roadmap](dependencies-node-roadmap.md).

## Customizing nodes

It is possible to modify the caption, icon and `ProjectTreeFlags` of nodes in the dependencies tree.

To do so, export an instance of `IProjectTreePropertiesProvider` as shown below. This example will rename the "Projects" node to "Applications":

```c#
[Export(ReferencesProjectTreeCustomizablePropertyValues.ContractName, typeof(IProjectTreePropertiesProvider))]
[AppliesTo(ProjectCapabilities.AlwaysApplicable)]
internal sealed class MyDependenciesTreePropertiesProvider : IProjectTreePropertiesProvider
{
    private static readonly ProjectTreeFlags ProjectDependencyGroup = ProjectTreeFlags.Create("ProjectDependencyGroup");

    public void CalculatePropertyValues(
        IProjectTreeCustomizablePropertyContext propertyContext,
        IProjectTreeCustomizablePropertyValues propertyValues)
    {
        if (propertyValues.Flags.Contains(ProjectDependencyGroup))
        {
            if (propertyValues is ReferencesProjectTreeCustomizablePropertyValues values)
            {
                // Change the caption (should be a localized string in production code)
                values.Caption = "Applications";

                // Change the icon
                values.Icon = values.ExpandedIcon = KnownMonikers.Application.ToProjectSystemType();
            }
        }
    }
}
```
