// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
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
            ProjectProperties commonProps = null,
            IProjectInstancePropertiesProvider instanceProvider = null,
            Func<ProjectId> getActiveProjectId = null)
            : this(workspace ?? WorkspaceFactory.Create(""),
                  unconfiguredProject: unconfiguredProject ?? IUnconfiguredProjectFactory.Create(),
                  interceptingProvider: interceptingProvider,
                  threadingService: threadingService,
                  defaultProperties: defaultProperties,
                  commonProps: commonProps,
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
            ProjectProperties commonProps = null,
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
                  projectProperties: commonProps ?? ProjectPropertiesFactory.CreateEmpty(),
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

            IProjectProperties defaultProperties = null;
            if (additionalProps != null)
            {
                var delegatePropertiesMock = IProjectPropertiesFactory.MockWithPropertiesAndValues(additionalProps);
                defaultProperties = delegatePropertiesMock.Object;
            }

            var commonProps = ProjectPropertiesFactory.Create(unconfiguredProject,
                new PropertyPageData { Category = ConfigurationGeneral.SchemaName, PropertyName = ConfigurationGeneral.SaveAssemblyInfoInSourceProperty, Value = true });
            return new TestProjectFileOrAssemblyInfoPropertiesProvider(unconfiguredProject, workspace: workspace, defaultProperties: defaultProperties,
                commonProps: commonProps, interceptingProvider: interceptingProvider);
        }

        private TestProjectFileOrAssemblyInfoPropertiesProvider CreateProviderForProjectFileValidation(
            string code,
            string propertyName,
            string propertyValueInProjectFile,
            out Workspace workspace,
            Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata> interceptingProvider = null,
            Dictionary<string, string> additionalProps = null)
        {
            workspace = WorkspaceFactory.Create(code);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(filePath: projectFilePath);

            additionalProps = additionalProps ?? new Dictionary<string, string>();
            additionalProps[propertyName] = propertyValueInProjectFile;
            var delegatePropertiesMock = IProjectPropertiesFactory.MockWithPropertiesAndValues(additionalProps);
            var defaultProperties = delegatePropertiesMock.Object;

            var commonProps = ProjectPropertiesFactory.Create(unconfiguredProject,
                new PropertyPageData { Category = ConfigurationGeneral.SchemaName, PropertyName = ConfigurationGeneral.SaveAssemblyInfoInSourceProperty, Value = false });

            return new TestProjectFileOrAssemblyInfoPropertiesProvider(unconfiguredProject, workspace: workspace, defaultProperties: defaultProperties,
                commonProps: commonProps, interceptingProvider: interceptingProvider);
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
        [InlineData(@"[assembly: System.Reflection.AssemblyCompanyAttribute(""MyCompany"")]", "AssemblyCompany", "MyCompany")]
        [InlineData(@"[assembly: System.Reflection.AssemblyProductAttribute(""MyProduct"")]", "Product", "MyProduct")]
        [InlineData(@"[assembly: System.Reflection.AssemblyVersionAttribute(""MyVersion"")]", "AssemblyVersion", "MyVersion")]
        [InlineData(@"[assembly: System.Resources.NeutralResourcesLanguageAttribute(""en-us"")]", "NeutralLanguage", "en-us")]
        // Negative cases
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "SomeProperty", null)]
        [InlineData(@"[assembly: System.Reflection.AssemblyDescriptionAttribute(""MyDescription"")]", "AssemblyCompany", null)]
        [InlineData(@"[assembly: System.Runtime.InteropServices.AssemblyDescriptionAttribute(true)]", "Description", null)]
        [InlineData(@"[assembly: System.Runtime.AssemblyDescriptionAttribute(""MyDescription"")]", "Description", null)]
        public async void SourceFileProperties_GetEvalutedPropertyAsync(string code, string propertyName, string expectedValue)
        {
            Workspace workspace;
            var provider = CreateProviderForSourceFileValidation(code, propertyName, out workspace);
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
            Workspace workspace;
            var provider = CreateProviderForProjectFileValidation(code, propertyName, propertyValueInProjectFile, out workspace);
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
            Workspace workspace;
            var provider = CreateProviderForSourceFileValidation(code, propertyName, out workspace);
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
            Workspace workspace;
            var provider = CreateProviderForProjectFileValidation(code, propertyName, existingPropertyValue, out workspace, additionalProps: propertyValues);
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
            Workspace workspace;
            var provider = CreateProviderForSourceFileValidation(code, propertyName, out workspace);
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
            Workspace workspace;
            var provider = CreateProviderForProjectFileValidation(code, propertyName, propertyValueInProjectFile, out workspace);
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
        [InlineData(@"", "PackageVersion", "1.0.0", typeof(PackageVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""1.1.1"")]", "PackageVersion", "1.1.1", typeof(PackageVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyFileVersionAttribute("""")]", "PackageVersion", "1.0.0", typeof(PackageVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""random"")]", "PackageVersion", "random", typeof(PackageVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""2.0.0"")]", "PackageVersion", "2.0.0", typeof(PackageVersionValueProvider))]
        [InlineData(@"[assembly: System.Reflection.AssemblyInformationalVersionAttribute(""2.0.1-beta1"")]", "PackageVersion", "2.0.1-beta1", typeof(PackageVersionValueProvider))]
        internal async void SourceFileProperties_DefaultValues_GetEvalutedPropertyAsync(string code, string propertyName, string expectedValue, Type interceptingProviderType)
        {
            var interceptingProvider = new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(
                valueFactory: () => (IInterceptingPropertyValueProvider)Activator.CreateInstance(interceptingProviderType),
                metadata: IInterceptingPropertyValueProviderMetadataFactory.Create(propertyName));
            Workspace workspace;
            var provider = CreateProviderForSourceFileValidation(code, propertyName, out workspace, interceptingProvider);
            var projectFilePath = workspace.CurrentSolution.Projects.First().FilePath;

            var properties = provider.GetProperties(projectFilePath, null, null);
            var propertyValue = await properties.GetEvaluatedPropertyValueAsync(propertyName);

            Assert.Equal(expectedValue, propertyValue);
        }

        [Theory]
        // PackageId
        [InlineData("MyApp", "PackageId", null, "MyApp", typeof(PackageIdValueProvider))]
        [InlineData("MyApp", "PackageId", "", "MyApp", typeof(PackageIdValueProvider))]
        [InlineData("MyApp", "PackageId", "ExistingValue", "ExistingValue", typeof(PackageIdValueProvider))]
        // Authors
        [InlineData("MyApp", "Authors", null, "MyApp", typeof(AuthorsValueProvider))]
        [InlineData("MyApp", "Authors", "", "MyApp", typeof(AuthorsValueProvider))]
        [InlineData("MyApp", "Authors", "ExistingValue", "ExistingValue", typeof(AuthorsValueProvider))]
        // Product
        [InlineData("MyApp", "Product", null, "MyApp", typeof(ProductValueProvider))]
        [InlineData("MyApp", "Product", "", "MyApp", typeof(ProductValueProvider))]
        [InlineData("MyApp", "Product", "ExistingValue", "ExistingValue", typeof(ProductValueProvider))]
        internal async void ProjectFileProperties_DefaultValues_GetEvalutedPropertyAsync(string assemblyName, string propertyName, string existingPropertyValue, string expectedValue, Type interceptingProviderType)
        {
            var interceptingProvider = new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(
                valueFactory: () => (IInterceptingPropertyValueProvider)Activator.CreateInstance(interceptingProviderType),
                metadata: IInterceptingPropertyValueProviderMetadataFactory.Create(propertyName));
            var additionalProps = new Dictionary<string, string>() { { "AssemblyName", assemblyName } };

            string code = "";
            Workspace workspace;
            var provider = CreateProviderForProjectFileValidation(code, propertyName, existingPropertyValue, out workspace, interceptingProvider, additionalProps);
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
        [InlineData("PackageVersion", null, "1.0.0", "1.0.0", typeof(PackageVersionValueProvider))]
        [InlineData("PackageVersion", null, "1.0.0-beta1", "1.0.0-beta1", typeof(PackageVersionValueProvider))]
        [InlineData("PackageVersion", "", "1.0.0.0", "1.0.0.0", typeof(PackageVersionValueProvider))]
        [InlineData("PackageVersion", "", "1.0.0-beta1", "1.0.0-beta1", typeof(PackageVersionValueProvider))]
        [InlineData("PackageVersion", null, "", "1.0.0", typeof(PackageVersionValueProvider))]
        [InlineData("PackageVersion", "1.0.0", "1.0.0", "1.0.0", typeof(PackageVersionValueProvider))]
        [InlineData("PackageVersion", "1.0.0-beta1", "1.0.0", "1.0.0", typeof(PackageVersionValueProvider))]
        [InlineData("PackageVersion", "1.0.0-beta1", "1.0.0-beta2", "1.0.0-beta2", typeof(PackageVersionValueProvider))]
        [InlineData("PackageVersion", "1.0.0", "1.0.0-beta1", "1.0.0-beta1", typeof(PackageVersionValueProvider))]
        [InlineData("PackageVersion", "1.1.1", "1.0.0.0", "1.0.0.0", typeof(PackageVersionValueProvider))]
        [InlineData("PackageVersion", "1.0.0", "1.0.0.0", "1.0.0.0", typeof(PackageVersionValueProvider))]
        internal async void ProjectFileProperties_WithInterception_SetEvalutedPropertyAsync(string propertyName, string existingPropertyValue, string propertyValueToSet, string expectedValue, Type interceptingProviderType)
        {
            var interceptingProvider = new Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>(
                valueFactory: () => (IInterceptingPropertyValueProvider)Activator.CreateInstance(interceptingProviderType),
                metadata: IInterceptingPropertyValueProviderMetadataFactory.Create(propertyName));
            
            string code = "";
            Workspace workspace;
            var provider = CreateProviderForProjectFileValidation(code, propertyName, existingPropertyValue, out workspace, interceptingProvider);
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
