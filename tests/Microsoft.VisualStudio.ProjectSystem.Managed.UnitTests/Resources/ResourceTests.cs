// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Utilities;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.Resources
{
    public sealed class ResourceTests
    {
        [Theory]
        [InlineData(@"src\Microsoft.VisualStudio.AppDesigner\Resources\Designer.Designer.vb", "Microsoft.VisualStudio.AppDesigner.Designer")]
        [InlineData(@"src\Microsoft.VisualStudio.Editors\AddImportsDialogs\AddImports.Designer.vb", "AddImports")]
        [InlineData(@"src\Microsoft.VisualStudio.Editors\OptionPages\GeneralOptionPageResources.Designer.vb", "GeneralOptionPageResources")]
        [InlineData(@"src\Microsoft.VisualStudio.Editors\PropPages\Strings.Designer.vb", "Strings")]
        [InlineData(@"src\Microsoft.VisualStudio.Editors\Resources\Microsoft.VisualStudio.Editors.Designer.Designer.vb", "Microsoft.VisualStudio.Editors.Designer")]
        [InlineData(@"src\Microsoft.VisualStudio.Editors\Resources\MyExtensibilityRes.Designer.vb", "MyExtensibilityRes")]
        [InlineData(@"src\Microsoft.VisualStudio.ProjectSystem.Managed\Resources.Designer.cs", "Microsoft.VisualStudio.Resources")]
        [InlineData(@"src\Microsoft.VisualStudio.ProjectSystem.Managed.VS\ProjectSystem\VS\PropertyPages\PropertyPageResources.Designer.cs", "Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.PropertyPageResources")]
        [InlineData(@"src\Microsoft.VisualStudio.ProjectSystem.Managed.VS\VSResources.Designer.cs", "Microsoft.VisualStudio.VSResources")]
        public void ResourceCodeGenHasCorrectBaseName(string sourcePath, string baseName)
        {
            // Resx code generation does not respect <LogicalName> metadata on <EmbeddedResource />.
            // This can result in an incorrect base name being used in generated code.
            //
            // https://github.com/dotnet/project-system/issues/1058
            //
            // To prevent this from happening unwittingly (eg. https://github.com/dotnet/project-system/issues/6180)
            // this test ensures the expected name is used. This will catch cases when code gen
            // produces invalid code before merging/insertion.

            string pattern = sourcePath.Substring(sourcePath.Length - 2, 2) switch
            {
                "cs" => $"global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager(\"{baseName}\"",
                "vb" => $"Dim temp As Global.System.Resources.ResourceManager = New Global.System.Resources.ResourceManager(\"{baseName}\"",
                string format => throw new Exception("Unexpected source file format: " + format)
            };

            string path = Path.Combine(RepoUtil.FindRepoRootPath(), sourcePath);

            foreach (string line in File.ReadLines(path))
            {
                if (line.Contains(pattern))
                    return;
            }

            throw new XunitException($"Expected base name \"{baseName}\" not found in generated file: {sourcePath}");
        }
    }
}
