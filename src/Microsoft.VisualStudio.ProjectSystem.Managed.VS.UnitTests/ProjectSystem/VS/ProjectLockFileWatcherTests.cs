// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class ProjectLockFileWatcherTests
    {
        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""", @"C:\Foo\obj\project.assets.json")]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    project.json, FilePath: ""C:\Foo\project.json""", @"C:\Foo\project.lock.json")]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    foo.project.json, FilePath: ""C:\Foo\foo.project.json""", @"C:\Foo\foo.project.lock.json")]
        public void VerifyFileWatcherRegistration(string inputTree, string fileToWatch)
        {
            var spMock = new IServiceProviderMoq();
            uint adviseCookie = 100;
            var fileChangeService = IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(adviseCookie);
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), fileChangeService);

            var propertyData = new PropertyPageData
            {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.BaseIntermediateOutputPathProperty,
                Value = "obj\\"
            };
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Foo\foo.proj");
            var watcher = new ProjectLockFileWatcher(spMock,
                                                     IProjectTreeProviderFactory.Create(),
                                                     IUnconfiguredProjectCommonServicesFactory.Create(unconfiguredProject,
                                                                                                      projectProperties: ProjectPropertiesFactory.Create(unconfiguredProject, new[] { propertyData })),
                                                     IProjectLockServiceFactory.Create());

            var tree = ProjectTreeParser.Parse(inputTree);
            watcher.Load();
            watcher.ProjectTree_ChangedAsync(IProjectVersionedValueFactory<IProjectTreeSnapshot>.Create(IProjectTreeSnapshotFactory.Create(tree)));

            // If fileToWatch is null then we expect to not register any filewatcher.
            var times = fileToWatch == null ? Times.Never() : Times.Once();
            Mock.Get<IVsFileChangeEx>(fileChangeService).Verify(s => s.AdviseFileChange(fileToWatch ?? It.IsAny<string>(), It.IsAny<uint>(), watcher, out adviseCookie), times);
        }

        [Theory]
        // Add file
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""", @"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    project.json, FilePath: ""C:\Foo\project.json""", 2, 1)]
        // Remove file
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    project.json, FilePath: ""C:\Foo\project.json""", @"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""", 2, 1)]
        // Rename file to projectName.project.json
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    project.json, FilePath: ""C:\Foo\project.json""", @"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    foo.project.json, FilePath: ""C:\Foo\foo.project.json""", 2, 1)]
        // Rename file to somethingelse
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    project.json, FilePath: ""C:\Foo\foo.project.json""", @"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    fooproject.json, FilePath: ""C:\Foo\fooproject.json""", 2, 1)]
        // Unrelated change
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    project.json, FilePath: ""C:\Foo\foo.project.json""", @"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    project.json, FilePath: ""C:\Foo\project.json""
    somefile.json, FilePath: ""C:\Foo\somefile.json""", 1, 0)]

        public void VerifyFileWatcherRegistrationOnTreeChange(string inputTree, string changedTree, int numRegisterCalls, int numUnregisterCalls)
        {
            var spMock = new IServiceProviderMoq();
            uint adviseCookie = 100;
            var fileChangeService = IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(adviseCookie);
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), fileChangeService);

            var propertyData = new PropertyPageData
            {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.BaseIntermediateOutputPathProperty,
                Value = "obj\\"
            };
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: @"C:\Foo\foo.proj");
            var watcher = new ProjectLockFileWatcher(spMock,
                                                     IProjectTreeProviderFactory.Create(),
                                                     IUnconfiguredProjectCommonServicesFactory.Create(unconfiguredProject,
                                                                                                      projectProperties: ProjectPropertiesFactory.Create(unconfiguredProject, new[] { propertyData })),
                                                     IProjectLockServiceFactory.Create());
            watcher.Load();

            var firstTree = ProjectTreeParser.Parse(inputTree);
            watcher.ProjectTree_ChangedAsync(IProjectVersionedValueFactory<IProjectTreeSnapshot>.Create(IProjectTreeSnapshotFactory.Create(firstTree)));

            var secondTree = ProjectTreeParser.Parse(changedTree);
            watcher.ProjectTree_ChangedAsync(IProjectVersionedValueFactory<IProjectTreeSnapshot>.Create(IProjectTreeSnapshotFactory.Create(secondTree)));

            // If fileToWatch is null then we expect to not register any filewatcher.
            Mock<IVsFileChangeEx> fileChangeServiceMock = Mock.Get<IVsFileChangeEx>(fileChangeService);
            fileChangeServiceMock.Verify(s => s.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), watcher, out adviseCookie), 
                                         Times.Exactly(numRegisterCalls));
            fileChangeServiceMock.Verify(s => s.UnadviseFileChange(adviseCookie), Times.Exactly(numUnregisterCalls));
        }
    }
}
