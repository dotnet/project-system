// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class LocalDebuggerCommandArgumentsProviderTests
    {
        private LocalDebuggerCommandArgumentsValueProvider CreateInstance(string targetExecutable = "", string outputKey = null, bool useOutputGroups = false)
        {
            var projectScope = IProjectCapabilitiesScopeFactory.Create();
            var configuredProject = ConfiguredProjectFactory.Create(projectScope);

            var configGeneralProperties = new PropertyPageData()
            {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.TargetPathProperty,
                Value = useOutputGroups ? string.Empty : targetExecutable
            };

            var project = IUnconfiguredProjectFactory.Create();
            var properties = ProjectPropertiesFactory.Create(project, configGeneralProperties);
            Lazy<IOutputGroupsService, IAppliesToMetadataView> outputGroupsWithData = null;

            if (useOutputGroups)
            {
                var metadata = new Mock<IAppliesToMetadataView>();
                metadata.Setup(m => m.AppliesTo).Returns("");
                var outputGroups = IOutputGroupsServiceFactory.Create(targetExecutable);
                outputGroupsWithData = new Lazy<IOutputGroupsService, IAppliesToMetadataView>(() => outputGroups, metadata.Object);
            }

            return new LocalDebuggerCommandArgumentsValueProvider(new Lazy<ProjectProperties>(() => properties), outputGroupsWithData, configuredProject);
        }

        [Fact]
        public void LocalDebuggerCommandArgumentsProvider_NullAsProjectProperites_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("projectProperties", () =>
            {
                new LocalDebuggerCommandArgumentsValueProvider(null, null, ConfiguredProjectFactory.Create());
            });
        }

        [Fact]
        public void LocalDebuggerCommandArgumentsProvider_NullAsConfiguredProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("configuredProject", () =>
            {
                new LocalDebuggerCommandArgumentsValueProvider(new Lazy<ProjectProperties>(() => ProjectPropertiesFactory.CreateEmpty()), null, null);
            });
        }

        [Theory]
        [InlineData("consoleapp1.exe", "consoleapp1", false, "arg1 arg2")]
        [InlineData("consoleapp1.exe", "consoleapp1", true, "arg1 arg2")]
        public async Task LocalDebuggerCommandArgumentsProvider_EmptyCommand_ReturnsExec(string executable, string dll, bool useOutputGroups, string args)
        {
            var properites = IProjectPropertiesFactory.CreateWithPropertyAndValue(WindowsLocalDebugger.LocalDebuggerCommandProperty, "");
            var directory = Directory.GetCurrentDirectory();
            var debugger = CreateInstance(executable, executable, useOutputGroups);
            Assert.Equal(
                $"exec {directory + Path.DirectorySeparatorChar + dll}.dll {args}",
                await debugger.OnGetEvaluatedPropertyValueAsync(args, properites)
                );
        }

        [Theory]
        [InlineData("dotnet.exe", "arg1 arg2 arg3")]
        [InlineData("csc.exe", "arg1 arg2 arg3")]
        [InlineData("nonexisting.exe", "arg1 arg2 arg3")]
        public async Task LocalDebuggerCommandArgumentsProvider_NonEmptyCommand_ReturnsUnmodifiedArgs(string command, string args)
        {
            var properties = IProjectPropertiesFactory.CreateWithPropertyAndValue(WindowsLocalDebugger.LocalDebuggerCommandProperty, command);
            var debugger = CreateInstance();
            Assert.Equal(args, await debugger.OnGetEvaluatedPropertyValueAsync(args, properties));
        }
    }
}
