// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Workspaces;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal class TestSourceFilePropertiesProvider : AbstractSourceFilePropertiesProvider
    {
        public TestSourceFilePropertiesProvider(UnconfiguredProject unconfiguredProject, Workspace workspace, IProjectThreadingService threadingService) 
            : base(unconfiguredProject, workspace, threadingService)
        {
        }
    }

    [ProjectSystemTrait]
    public class SourceFilePropertiesProviderTests
    {
        [Fact]
        public void Constructor_NullUnconfiguredProject_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () =>
            {
                new TestSourceFilePropertiesProvider(null, WorkspaceFactory.Create(""), IProjectThreadingServiceFactory.Create());
            });
        }

        [Fact]
        public void Constructor_NullWorkspace_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("workspace", () =>
            {
                new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(), null, IProjectThreadingServiceFactory.Create());
            });
        }

        [Fact]
        public void Constructor_NullThreadingFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () =>
            {
                new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(), WorkspaceFactory.Create(""), null);
            });
        }

        [Fact]
        public void DefaultProjectPath()
        {
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(filePath: "D:\\TestFile"), WorkspaceFactory.Create(""), IProjectThreadingServiceFactory.Create());
            Assert.Equal(provider.DefaultProjectPath, "D:\\TestFile");
        }

        [Fact]
        public void GetItemProperties_ThrowsInvalidOperationException()
        {
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(), WorkspaceFactory.Create(""), IProjectThreadingServiceFactory.Create());
            Assert.Throws<InvalidOperationException>(() => provider.GetItemProperties(null, null));
        }

        [Fact]
        public void GetItemTypeProperties_ThrowsInvalidOperationException()
        {
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(), WorkspaceFactory.Create(""), IProjectThreadingServiceFactory.Create());
            Assert.Throws<InvalidOperationException>(() => provider.GetItemTypeProperties(null));
        }

        [Fact]
        public void GetProperties_NotNull()
        {
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(), WorkspaceFactory.Create(""), IProjectThreadingServiceFactory.Create());
            var properties = provider.GetProperties(null, null, null);
            Assert.NotNull(properties);
        }

        [Theory]
        [InlineData(@"[assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"")]", "Title", "MyTitle")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", "MyDescription")]
        [InlineData(@"[assembly: System.Reflection.AssemblyCompanyAttribute(""MyCompany"")]", "Company", "MyCompany")]
        [InlineData(@"[assembly: System.Reflection.AssemblyProductAttribute(""MyProduct"")]", "Product", "MyProduct")]
        [InlineData(@"[assembly: System.Reflection.AssemblyCopyrightAttribute(""MyCopyright"")]", "Copyright", "MyCopyright")]
        [InlineData(@"[assembly: System.Reflection.AssemblyTrademarkAttribute(""MyTrademark"")]", "Trademark", "MyTrademark")]
        [InlineData(@"[assembly: System.Reflection.AssemblyVersionAttribute(""MyVersion"")]", "AssemblyVersion", "MyVersion")]
        [InlineData(@"[assembly: System.Resources.NeutralResourcesLanguageAttribute(""en-us"")]", "NeutralResourcesLanguage", "en-us")]
        [InlineData(@"[assembly: System.Runtime.InteropServices.GuidAttribute(""SomeGuid"")]", "AssemblyGuid", "SomeGuid")]
        [InlineData(@"[assembly: System.Runtime.InteropServices.ComVisibleAttribute(true)]", "ComVisible", "True")]
        // VB
        [InlineData(@"<Assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"")>", "Title", "MyTitle")]
        [InlineData(@"<Assembly: System.Runtime.InteropServices.ComVisibleAttribute(true)>", "ComVisible", "True")]
        // Negative cases
        [InlineData(@"[assembly: System.Runtime.InteropServices.ComVisibleAttribute(true)]", "SomeProperty", null)]
        [InlineData(@"[assembly: System.Runtime.InteropServices.ComVisibleAttribute(true)]", "Title", null)]
        [InlineData(@"[assembly: System.Runtime.InteropServices.ComVisibleAttribute(true, false)]", "ComVisible", null)]
        [InlineData(@"[assembly: System.Runtime.ComVisibleAttribute(true)]", "ComVisible", null)]
        public async void SourceFileProperties_GetEvalutedPropertyAsync(string code, string propertyName, string expectedValue)
        {
            var language = code.Contains("[") ? LanguageNames.CSharp : LanguageNames.VisualBasic;
            var workspace = WorkspaceFactory.Create(code, language);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(filePath: projectFilePath), workspace, IProjectThreadingServiceFactory.Create());

            var properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(propertyName);

            Assert.Equal(expectedValue, propertyValue);
        }

        [Theory]
        [InlineData(@"[assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"")]", "Title", "NewTitle",
                    @"[assembly: System.Reflection.AssemblyTitleAttribute(""NewTitle"")]")]
        [InlineData(@"[assembly: System.Runtime.InteropServices.ComVisibleAttribute(true)]", "ComVisible", "false",
                    @"[assembly: System.Runtime.InteropServices.ComVisibleAttribute(false)]")]
        [InlineData(@"<Assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"")>", "Title", "NewTitle",
                    @"<Assembly: System.Reflection.AssemblyTitleAttribute(""NewTitle"")>")]
        [InlineData(@"<Assembly: System.Runtime.InteropServices.ComVisibleAttribute(True)>", "ComVisible", "false",
                    @"<Assembly: System.Runtime.InteropServices.ComVisibleAttribute(False)>")]
        [InlineData(@"[assembly: System.Reflection.AssemblyTitleAttribute(/*Trivia*/ ""MyTitle"" /*Trivia*/)]", "Title", "NewTitle",
                    @"[assembly: System.Reflection.AssemblyTitleAttribute(/*Trivia*/ ""NewTitle"" /*Trivia*/)]")]
        [InlineData(@"<Assembly: System.Reflection.AssemblyTitleAttribute(    ""MyTitle""     )>", "Title", "NewTitle",
                    @"<Assembly: System.Reflection.AssemblyTitleAttribute(    ""NewTitle""     )>")]
        //Negative cases
        [InlineData(@"[assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"")]", "Description", "NewTitle",
                    @"[assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"")]")]
        [InlineData(@"[assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"")]", "SomeRandomPropety", "NewTitle",
                    @"[assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"")]")]
        [InlineData(@"[assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"", ""MyDescription"")]", "Title", "NewTitle",
                    @"[assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"", ""MyDescription"")]")]
        [InlineData(@"[assembly: System.AssemblyTitleAttribute(""MyTitle"")]", "Title", "NewTitle",
                    @"[assembly: System.AssemblyTitleAttribute(""MyTitle"")]")]
        public async void SourceFileProperties_SetPropertyValueAsync(string code, string propertyName, string propertyValue, string expectedCode)
        {
            var language = code.Contains("[") ? LanguageNames.CSharp : LanguageNames.VisualBasic;
            var workspace = WorkspaceFactory.Create(code, language);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(filePath: projectFilePath), workspace, IProjectThreadingServiceFactory.Create());

            var properties = provider.GetProperties(projectFilePath, null, null);
            await properties.SetPropertyValueAsync(propertyName, propertyValue);

            var newCode = (await workspace.CurrentSolution.Projects.First().Documents.First().GetTextAsync()).ToString();
            Assert.Equal(expectedCode, newCode);
        }

        [Theory]
        [InlineData(@"[assembly: System.Reflection.AssemblyTitleAttribute(""MyTitle"")]", "Title", "MyTitle")]
        public async void SourceFileProperties_GetUnevalutedPropertyAsync(string code, string propertyName, string expectedValue)
        {
            var workspace = WorkspaceFactory.Create(code);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;
            var provider = new TestSourceFilePropertiesProvider(IUnconfiguredProjectFactory.Create(filePath: projectFilePath), workspace, IProjectThreadingServiceFactory.Create());

            var properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetUnevaluatedPropertyValueAsync(propertyName);

            Assert.Equal(expectedValue, propertyValue);
        }
    }
}
