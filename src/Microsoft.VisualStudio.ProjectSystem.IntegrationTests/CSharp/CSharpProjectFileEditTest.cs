// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpProjectFileEditTest : AbstractIntegrationTest
    {
        protected override string DefaultLanguageName => LanguageNames.CSharp;

        public CSharpProjectFileEditTest(VisualStudioInstanceFactory instanceFactory)
            : base(nameof(CSharpProjectFileEditTest), WellKnownProjectTemplates.CSharpNetCoreConsoleApplication, instanceFactory)
        {
            VisualStudio.SolutionExplorer.OpenFile(Project, "Program.cs");

            VisualStudio.Editor.SetText(@"using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectFileEditTest
{
    /// <summary/>
    public class Program
    {
        /// <summary/>
        public static void Main(string[] args)
        {
            Console.WriteLine(""Hello World"");
        }
    }
}
");
        }

        [Fact, Trait("Integration", "ProjectFileEdit")]
        public void EditNetCore10To11()
        {
            VisualStudio.SolutionExplorer.EditProjectFile(Project);

            VisualStudio.Editor.SetText(@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

</Project>");

            VisualStudio.SolutionExplorer.SaveAll();
            VisualStudio.SolutionExplorer.CloseFile(Project, "Program.cs", saveFile: true);

            VisualStudio.SolutionExplorer.RestoreNuGetPackages();

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            VisualStudio.WaitForApplicationIdle();

            VisualStudio.SolutionExplorer.BuildSolution(waitForBuildToFinish: true);
        }

        [Fact, Trait("Integration", "ProjectFileEdit")]
        public void AddPackageReference()
        {
            VisualStudio.SolutionExplorer.EditProjectFile(Project);

            VisualStudio.Editor.SetText(@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""9.0.1"" />
  </ItemGroup>

</Project>");

            VisualStudio.SolutionExplorer.SaveAll();
            VisualStudio.SolutionExplorer.CloseFile(Project, "Program.cs", saveFile: true);

            VisualStudio.SolutionExplorer.RestoreNuGetPackages();

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            VisualStudio.WaitForApplicationIdle();

            VisualStudio.SolutionExplorer.BuildSolution(waitForBuildToFinish: true);
        }
    }
}
