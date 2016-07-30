// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Moq;
using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
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
    }
}
