// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    [ProjectSystemTrait]
    public class BuildMacroInfoTests
    {
        [Theory]
        [InlineData("MyBuildMacro", "MyBuildMacroValue", VSConstants.S_OK)]
        [InlineData("NonExistantMacro", "", VSConstants.E_FAIL)]
        public void BuildMacroInfoTests_GetBuildMacroValue(string macroName, string expectedValue, int expectedRetVal)
        {
            var projectProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue("MyBuildMacro", "MyBuildMacroValue");
            var propertiesProvider = IProjectPropertiesProviderFactory.Create(commonProps: projectProperties);
            var configuredProjectServices = Mock.Of<IConfiguredProjectServices>(o =>
                o.ProjectPropertiesProvider == propertiesProvider);
            var configuredProject = ConfiguredProjectFactory.Create(services: configuredProjectServices);
            ActiveConfiguredProject<ConfiguredProject> activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => configuredProject);
            var threadingService = IProjectThreadingServiceFactory.Create();

            var buildMacroInfo = new BuildMacroInfo(activeConfiguredProject, threadingService);

            string macroValue;
            int retVal = buildMacroInfo.GetBuildMacroValue(macroName, out macroValue);
            Assert.Equal(expectedRetVal, retVal);
            Assert.Equal(expectedValue, macroValue);
        }
    }
}
