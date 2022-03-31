// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public class ProjectCapabilitiesMissingVetoProjectLoadTests
    {
        [Theory]
        [InlineData("Project.vbproj")]
        [InlineData("Project.csproj")]
        [InlineData("Project.fsproj")]
        public async Task AllowProjectLoadAsync_WhenMissingCapability_ReturnsFail(string fullPath)
        {
            var project = UnconfiguredProjectFactory.ImplementFullPath(fullPath);
            var capabilitiesService = IProjectCapabilitiesServiceFactory.ImplementsContains(c => false);

            var veto = CreateInstance(project, capabilitiesService);

            var result = await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return veto.AllowProjectLoadAsync(true, ProjectConfigurationFactory.Create("Debug|AnyCPU"), CancellationToken.None);
            });

            Assert.Equal(VSConstants.E_FAIL, result.HResult);
        }

        [Theory]
        [InlineData("Project.vbproj")]
        [InlineData("Project.csproj")]
        [InlineData("Project.fsproj")]
        public async Task AllowProjectLoadAsync_WhenContainsCapability_ReturnsTrue(string fullPath)
        {
            var project = UnconfiguredProjectFactory.ImplementFullPath(fullPath);
            var capabilitiesService = IProjectCapabilitiesServiceFactory.ImplementsContains(c => true);

            var veto = CreateInstance(project, capabilitiesService);

            var result = await veto.AllowProjectLoadAsync(true, ProjectConfigurationFactory.Create("Debug|AnyCPU"), CancellationToken.None);

            Assert.True(result);
        }

        [Theory]
        [InlineData("Project.vcxproj")]
        [InlineData("Project.shproj")]
        [InlineData("Project.androidproj")]
        public async Task AllowProjectLoadAsync_WhenUnrecognizedExtension_ReturnsTrue(string fullPath)
        {
            var project = UnconfiguredProjectFactory.ImplementFullPath(fullPath);
            var capabilitiesService = IProjectCapabilitiesServiceFactory.ImplementsContains(c => throw new Exception("Should not be hit"));

            var veto = CreateInstance(project, capabilitiesService);

            var result = await veto.AllowProjectLoadAsync(true, ProjectConfigurationFactory.Create("Debug|AnyCPU"), CancellationToken.None);

            Assert.True(result);
        }

        private static ProjectCapabilitiesMissingVetoProjectLoad CreateInstance(UnconfiguredProject project, IProjectCapabilitiesService projectCapabilitiesService)
        {
            return new ProjectCapabilitiesMissingVetoProjectLoad(project, projectCapabilitiesService);
        }
    }
}
