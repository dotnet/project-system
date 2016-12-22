// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Properties.Package;
using Microsoft.VisualStudio.Workspaces;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal class TestProjectFileOrAssemblyInfoPropertiesProvider : AbstractProjectFileOrAssemblyInfoPropertiesProvider
    {
        public TestProjectFileOrAssemblyInfoPropertiesProvider(
            UnconfiguredProject unconfiguredProject = null,
            Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> interceptingProvider = null,
            Workspace workspace = null,
            IProjectThreadingService threadingService = null,
            IProjectProperties defaultProperties = null,
            IProjectInstancePropertiesProvider instanceProvider = null,
            Func<ProjectId> getActiveProjectId = null)
            : this(workspace ?? WorkspaceFactory.Create(""),
                  unconfiguredProject: unconfiguredProject ?? IUnconfiguredProjectFactory.Create(),
                  interceptingProvider: interceptingProvider,
                  threadingService: threadingService,
                  defaultProperties: defaultProperties,
                  instanceProvider: instanceProvider,
                  getActiveProjectId: getActiveProjectId)
        {
        }

        public TestProjectFileOrAssemblyInfoPropertiesProvider(
            Workspace workspace,
            UnconfiguredProject unconfiguredProject,
            Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> interceptingProvider = null,
            IProjectThreadingService threadingService = null,
            IProjectProperties defaultProperties = null,
            IProjectInstancePropertiesProvider instanceProvider = null,
            Func<ProjectId> getActiveProjectId = null)
            : base(delegatedProvider: IProjectPropertiesProviderFactory.Create(defaultProperties ?? IProjectPropertiesFactory.MockWithProperty("").Object),
                  instanceProvider: instanceProvider ?? IProjectInstancePropertiesProviderFactory.Create(),
                  interceptingValueProviders: interceptingProvider == null ?
                    new[] { new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(
                        () => IInterceptingPropertyValueProviderFactory.Create(),
                        IInterceptingPropertyValueProviderMetadataFactory.Create("")) } :
                    new[] { interceptingProvider },
                  unconfiguredProject: unconfiguredProject,
                  getActiveProjectId: getActiveProjectId ?? (() => workspace.CurrentSolution.ProjectIds.SingleOrDefault()),
                  workspace: workspace,
                  threadingService: threadingService ?? IProjectThreadingServiceFactory.Create())
        {
            Requires.NotNull(workspace, nameof(workspace));
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
        }
    }

    [ProjectSystemTrait]
    public class AssemblyInfoPropertiesProviderTests
    {
        private TestProjectFileOrAssemblyInfoPropertiesProvider CreateProviderForSourceFileValidation(
            string code,
            string propertyName,
            out Workspace workspace,
            Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> interceptingProvider = null,
            Dictionary<string, string> additionalProps = null)
        {
            var language = code.Contains("[") ? LanguageNames.CSharp : LanguageNames.VisualBasic;
            workspace = WorkspaceFactory.Create(code, language);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(filePath: projectFilePath);
            var defaultProperties = CreateProjectProperties(additionalProps, saveInProjectFile: false);
            return new TestProjectFileOrAssemblyInfoPropertiesProvider(unconfiguredProject, workspace: workspace, defaultProperties: defaultProperties, interceptingProvider: interceptingProvider);
        }

        private TestProjectFileOrAssemblyInfoPropertiesProvider CreateProviderForProjectFileValidation(
            string code,
            string propertyName,
            string propertyValueInProjectFile,
            out Workspace workspace,
            Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> interceptingProvider = null,
            Dictionary<string, string> additionalProps = null)
        {
            var language = code.Contains("[") ? LanguageNames.CSharp : LanguageNames.VisualBasic;
            workspace = WorkspaceFactory.Create(code, language);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(filePath: projectFilePath);
            additionalProps = additionalProps ?? new Dictionary<string, string>();
            additionalProps[propertyName] = propertyValueInProjectFile;
            var defaultProperties = CreateProjectProperties(additionalProps, saveInProjectFile: true);
            return new TestProjectFileOrAssemblyInfoPropertiesProvider(unconfiguredProject, workspace: workspace, defaultProperties: defaultProperties, interceptingProvider: interceptingProvider);
        }

        private IProjectProperties CreateProjectProperties(Dictionary<string, string> additionalProps, bool saveInProjectFile)
        {
            additionalProps = additionalProps ?? new Dictionary<string, string>();

            // Configure whether AssemblyInfo properties are generated in project file or not.
            var saveInProjectFileStr = saveInProjectFile.ToString();
            foreach (var kvp in AssemblyInfoProperties.s_assemblyPropertyInfoMap)
            {
                var generatePropertyInProjectFileName = kvp.Value.GeneratePropertyInProjectFileName;
                additionalProps[generatePropertyInProjectFileName] = saveInProjectFileStr;
            }

            additionalProps["GenerateAssemblyInfo"] = saveInProjectFileStr;

            return IProjectPropertiesFactory.MockWithPropertiesAndValues(additionalProps).Object;
        }

        [Fact]
        public void DefaultProjectPath()
        {
            var provider = new TestProjectFileOrAssemblyInfoPropertiesProvider(IUnconfiguredProjectFactory.Create(filePath: "D:\\TestFile"));
            Assert.Equal("D:\\TestFile", provider.DefaultProjectPath);
        }

        [Fact]
        public void GetProperties_NotNull()
        {
            var provider = new TestProjectFileOrAssemblyInfoPropertiesProvider();
            var properties = provider.GetProperties(null, null, null);
            Assert.NotNull(properties);
        }

        [Theory]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", "MyDescription")]
        [InlineData(@"[assembly: System.Reflection.AssemblyCompanyAttribute(""MyCompany"")]", "Company", "MyCompany")]
        [InlineData(@"[assembly: System.Reflection.AssemblyProductAttribute(""MyProduct"")]", "Product", "MyProduct")]
        [InlineData(@"[assembly: System.Reflection.AssemblyVersionAttribute(""MyVersion"")]", "AssemblyVersion", "MyVersion")]
        [InlineData(@"[assembly: System.Resources.NeutralResourcesLanguageAttribute(""en-us"")]", "NeutralLanguage", "en-us")]
        // Negative cases
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "SomeProperty", null)]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Company", null)]
        [InlineData(@"[assembly: System.Runtime.InteropServices.AssemblyDescriptionAttribute(true)]", "Description", null)]
        [InlineData(@"[assembly: System.Runtime.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", null)]
        public async void SourceFileProperties_GetEvalutedPropertyAsync(string code, string propertyName, string expectedValue)
        {
            var provider = CreateProviderForSourceFileValidation(code, propertyName, out Workspace workspace);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;

            var properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(propertyName);

            Assert.Equal(expectedValue, propertyValue);
        }

        [Theory]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", "MyDescription", "MyDescription")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", "MyDescription2", "MyDescription2")]
        [InlineData("", "Description", "MyDescription", "MyDescription")]
        [InlineData(@"[assembly: System.Reflection.AssemblyCompanyAttribute(""MyCompany"")]", "Company", "MyCompany2", "MyCompany2")]
        [InlineData(@"[assembly: System.Reflection.AssemblyProductAttribute(""MyProduct"")]", "Product", "MyProduct2", "MyProduct2")]
        [InlineData(@"[assembly: System.Reflection.AssemblyVersionAttribute(""MyVersion"")]", "AssemblyVersion", "MyVersion2", "MyVersion2")]
        [InlineData(@"[assembly: System.Resources.NeutralResourcesLanguageAttribute(""en-us"")]", "NeutralResourcesLanguage", "en-uk", "en-uk")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(true)]", "Description", "MyDescription", "MyDescription")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription""]", "Description", "", "")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription""]", "Description", null, null)]
        public async void ProjectFileProperties_GetEvalutedPropertyAsync(string code, string propertyName, string propertyValueInProjectFile, string expectedValue)
        {
            var provider = CreateProviderForProjectFileValidation(code, propertyName, propertyValueInProjectFile, out Workspace workspace);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;

            var properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(propertyName);

            Assert.Equal(expectedValue, propertyValue);
        }

        [Theory]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", "NewDescription",
                    @"[assembly: System.Reflection.AssemblyDescriptionAttribute(""NewDescription"")]")]
        [InlineData(@"<Assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")>", "Description", "NewDescription",
                    @"<Assembly: System.Reflection.AssemblyDescriptionAttribute(""NewDescription"")>")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(/*Trivia*/ ""MyDescription"" /*Trivia*/)]", "Description", "NewDescription",
                    @"[assembly: System.Reflection.AssemblyDescriptionAttribute(/*Trivia*/ ""NewDescription"" /*Trivia*/)]")]
        [InlineData(@"<Assembly: System.Reflection.AssemblyDescriptionAttribute(    ""MyDescription""     )>", "Description", "NewDescription",
                    @"<Assembly: System.Reflection.AssemblyDescriptionAttribute(    ""NewDescription""     )>")]
        //Negative cases
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Product", "NewDescription",
                    @"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "SomeRandomPropety", "NewDescription",
                    @"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"", ""MyDescription"")]", "Description", "NewDescription",
                    @"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"", ""MyDescription"")]")]
        [InlineData(@"[assembly: System.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", "NewDescription",
                    @"[assembly: System.AssemblyDescriptionAttribute(""MyDescription"")]")]
        public async void SourceFileProperties_SetPropertyValueAsync(string code, string propertyName, string propertyValue, string expectedCode)
        {
            var provider = CreateProviderForSourceFileValidation(code, propertyName, out Workspace workspace);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;

            var properties = provider.GetProperties(projectFilePath, null, null);
            await properties.SetPropertyValueAsync(propertyName, propertyValue);

            var newCode = (await workspace.CurrentSolution.Projects.First().Documents.First().GetTextAsync()).ToString();
            Assert.Equal(expectedCode, newCode);
        }

        [Theory]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", "", "NewDescription", "NewDescription")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", null, "", "")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", null, "NewDescription", "NewDescription")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", "OldDescription", "NewDescription", "NewDescription")]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"", ""MyDescription"")]", "Description", "OldDescription", "", "")]
        public async void ProjectFileProperties_SetPropertyValueAsync(string code, string propertyName, string existingPropertyValue, string propertyValueToSet, string expectedValue)
        {
            var propertyValues = new Dictionary<string, string>();
            var provider = CreateProviderForProjectFileValidation(code, propertyName, existingPropertyValue, out Workspace workspace, additionalProps: propertyValues);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;

            var properties = provider.GetProperties(projectFilePath, null, null);

            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(propertyName);
            Assert.Equal(existingPropertyValue, propertyValue);

            await properties.SetPropertyValueAsync(propertyName, propertyValueToSet);

            // Confirm the new value.
            propertyValue = propertyValues[propertyName];
            Assert.Equal(expectedValue, propertyValue);

            // Verify no code changes as property was written to project file.
            var newCode = (await workspace.CurrentSolution.Projects.First().Documents.First().GetTextAsync()).ToString();
            Assert.Equal(code, newCode);
        }

        [Theory]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", "MyDescription")]
        public async void SourceFileProperties_GetUnevalutedPropertyAsync(string code, string propertyName, string expectedValue)
        {
            var provider = CreateProviderForSourceFileValidation(code, propertyName, out Workspace workspace);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;

            var properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetUnevaluatedPropertyValueAsync(propertyName);

            Assert.Equal(expectedValue, propertyValue);
        }

        [Theory]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", "MyDescription2", "MyDescription2")]
        [InlineData("", "Description", "MyDescription", "MyDescription")]
        public async void ProjectFileProperties_GetUnevalutedPropertyAsync(string code, string propertyName, string propertyValueInProjectFile, string expectedValue)
        {
            var provider = CreateProviderForProjectFileValidation(code, propertyName, propertyValueInProjectFile, out Workspace workspace);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;

            var properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetUnevaluatedPropertyValueAsync(propertyName);

            Assert.Equal(expectedValue, propertyValue);
        }

        [Theory]
        // AssemblyVersion
        [InlineData(@"", "AssemblyVersion", "1.0.0.0", typeof(AssemblyVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyVersionAttribute(""1.1.1"")]", "AssemblyVersion", "1.1.1", typeof(AssemblyVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyVersionAttribute("""")]", "AssemblyVersion", "1.0.0.0", typeof(AssemblyVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyVersionAttribute(""random"")]", "AssemblyVersion", "random", typeof(AssemblyVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""2.0.0"")]", "AssemblyVersion", "2.0.0.0", typeof(AssemblyVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""2.0.1-beta1"")]", "AssemblyVersion", "2.0.1.0", typeof(AssemblyVersionValueProvider))]
        // FileVersion
        [InlineData(@"", "FileVersion", "1.0.0.0", typeof(FileVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyFileVersionAttribute(""1.1.1"")]", "FileVersion", "1.1.1", typeof(FileVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyFileVersionAttribute("""")]", "FileVersion", "1.0.0.0", typeof(FileVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyFileVersionAttribute(""random"")]", "FileVersion", "random", typeof(FileVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""2.0.0"")]", "FileVersion", "2.0.0.0", typeof(FileVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""2.0.1-beta1"")]", "FileVersion", "2.0.1.0", typeof(FileVersionValueProvider))]
        // PackageVersion
        [InlineData(@"", "Version", null, null)]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""1.1.1"")]", "Version", "1.1.1", null)]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute("""")]", "Version", "", null)]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""random"")]", "Version", "random", null)]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""2.0.0"")]", "Version", "2.0.0", null)]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""2.0.1-beta1"")]", "Version", "2.0.1-beta1", null)]
        internal async void SourceFileProperties_DefaultValues_GetEvalutedPropertyAsync(string code, string propertyName, string expectedValue, Type interceptingProviderType)
        {
            var interceptingProvider = interceptingProviderType != null ?
                new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(
                    valueFactory: () => (IInterceptingPropertyValueProvider)Activator.CreateInstance(interceptingProviderType),
                    metadata: IInterceptingPropertyValueProviderMetadataFactory.Create(propertyName)) :
                null;
            var provider = CreateProviderForSourceFileValidation(code, propertyName, out Workspace workspace, interceptingProvider);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;

            var properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(propertyName);

            Assert.Equal(expectedValue, propertyValue);
        }

        [Theory]
        // PackageId
        [InlineData("MyApp", "PackageId", null, null)]
        [InlineData("MyApp", "PackageId", "", "")]
        [InlineData("MyApp", "PackageId", "ExistingValue", "ExistingValue")]
        // Authors
        [InlineData("MyApp", "Authors", null, null)]
        [InlineData("MyApp", "Authors", "", "")]
        [InlineData("MyApp", "Authors", "ExistingValue", "ExistingValue")]
        // Product
        [InlineData("MyApp", "Product", null, null)]
        [InlineData("MyApp", "Product", "", "")]
        [InlineData("MyApp", "Product", "ExistingValue", "ExistingValue")]
        internal async void ProjectFileProperties_DefaultValues_GetEvalutedPropertyAsync(string assemblyName, string propertyName, string existingPropertyValue, string expectedValue)
        {
            var additionalProps = new Dictionary<string, string>() { { "AssemblyName", assemblyName } };

            string code = "";
            var provider = CreateProviderForProjectFileValidation(code, propertyName, existingPropertyValue, out Workspace workspace, additionalProps: additionalProps);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;

            var properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(propertyName);

            Assert.Equal(expectedValue, propertyValue);
        }

        [Theory]
        // AssemblyVersion
        [InlineData("AssemblyVersion", null, "1.0.0.0", "1.0.0.0", typeof(AssemblyVersionValueProvider))]
        [InlineData("AssemblyVersion", "", "1.0.0.0", "1.0.0.0", typeof(AssemblyVersionValueProvider))]
        [InlineData("AssemblyVersion", null, "", "1.0.0.0", typeof(AssemblyVersionValueProvider))]
        [InlineData("AssemblyVersion", "1.0.0.0", "1.0.0.0", "1.0.0.0", typeof(AssemblyVersionValueProvider))]
        [InlineData("AssemblyVersion", "1.1.1", "1.0.0.0", "1.0.0.0", typeof(AssemblyVersionValueProvider))]
        [InlineData("AssemblyVersion", "1.0.0.0", "1.0.0", "1.0.0", typeof(AssemblyVersionValueProvider))]
        // FileVersion
        [InlineData("FileVersion", null, "1.0.0.0", "1.0.0.0", typeof(FileVersionValueProvider))]
        [InlineData("FileVersion", "", "1.0.0.0", "1.0.0.0", typeof(FileVersionValueProvider))]
        [InlineData("FileVersion", null, "", "1.0.0.0", typeof(FileVersionValueProvider))]
        [InlineData("FileVersion", "1.0.0.0", "1.0.0.0", "1.0.0.0", typeof(FileVersionValueProvider))]
        [InlineData("FileVersion", "1.1.1", "1.0.0.0", "1.0.0.0", typeof(FileVersionValueProvider))]
        [InlineData("FileVersion", "1.0.0.0", "1.0.0", "1.0.0", typeof(FileVersionValueProvider))]
        // PackageVersion
        [InlineData("Version", null, "1.0.0", "1.0.0", null)]
        [InlineData("Version", null, "1.0.0-beta1", "1.0.0-beta1", null)]
        [InlineData("Version", "", "1.0.0.0", "1.0.0.0", null)]
        [InlineData("Version", "", "1.0.0-beta1", "1.0.0-beta1", null)]
        [InlineData("Version", "1.0.0", "1.0.0", "1.0.0", null)]
        [InlineData("Version", "1.0.0-beta1", "1.0.0", "1.0.0", null)]
        [InlineData("Version", "1.0.0-beta1", "1.0.0-beta2", "1.0.0-beta2", null)]
        [InlineData("Version", "1.0.0", "1.0.0-beta1", "1.0.0-beta1", null)]
        [InlineData("Version", "1.1.1", "1.0.0.0", "1.0.0.0", null)]
        [InlineData("Version", "1.0.0", "1.0.0.0", "1.0.0.0", null)]
        internal async void ProjectFileProperties_WithInterception_SetEvalutedPropertyAsync(string propertyName, string existingPropertyValue, string propertyValueToSet, string expectedValue, Type interceptingProviderType)
        {
            var interceptingProvider = interceptingProviderType != null ?
                new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(
                    valueFactory: () => (IInterceptingPropertyValueProvider)Activator.CreateInstance(interceptingProviderType),
                    metadata: IInterceptingPropertyValueProviderMetadataFactory.Create(propertyName)) :
                null;
            
            string code = "";
            var provider = CreateProviderForProjectFileValidation(code, propertyName, existingPropertyValue, out Workspace workspace, interceptingProvider);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;

            var properties = provider.GetProperties(projectFilePath, null, null);
            await properties.SetPropertyValueAsync(propertyName, propertyValueToSet);

            // Read the property value again and confirm the new value.
            properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(propertyName);
            Assert.Equal(expectedValue, propertyValue);

            // Verify no code changes as property was written to project file.
            var newCode = (await workspace.CurrentSolution.Projects.First().Documents.First().GetTextAsync()).ToString();
            Assert.Equal(code, newCode);
        }
    }
}
