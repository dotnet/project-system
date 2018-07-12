// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    [Trait("UnitTest", "ProjectSystem")]
    public class ProjectAssetFileWatcherTests
    {
        private const string ProjectCurrentStateJson = @"{
    ""CurrentState"": {
        ""ConfigurationGeneral"": {
            ""Properties"": {
               ""BaseIntermediateOutputPath"": ""obj\\"",
               ""MSBuildProjectFullPath"": ""C:\\Foo\\foo.proj""
            }
        }
    }
}";

        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""", @"C:\Foo\obj\project.assets.json")]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    project.json, FilePath: ""C:\Foo\project.json""", @"C:\Foo\project.lock.json")]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""
    foo.project.json, FilePath: ""C:\Foo\foo.project.json""", @"C:\Foo\foo.project.lock.json")]
        public async Task VerifyFileWatcherRegistration(string inputTree, string fileToWatch)
        {
            var spMock = new IAsyncServiceProviderMoq();
            uint adviseCookie = 100;
            var fileChangeService = IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(adviseCookie);
            spMock.AddService(typeof(SVsFileChangeEx), fileChangeService);
            var tasksService = IUnconfiguredProjectTasksServiceFactory.ImplementLoadedProjectAsync<ConfiguredProject>(t => t());

            var watcher = new ProjectAssetFileWatcher(spMock,
                                                     IProjectTreeProviderFactory.Create(),
                                                     IUnconfiguredProjectCommonServicesFactory.Create(threadingService: IProjectThreadingServiceFactory.Create()),
                                                     tasksService,
                                                     IActiveConfiguredProjectSubscriptionServiceFactory.Create());

            var tree = ProjectTreeParser.Parse(inputTree);
            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(ProjectCurrentStateJson);
            watcher.Load();
            await watcher.DataFlow_ChangedAsync(IProjectVersionedValueFactory<Tuple<IProjectTreeSnapshot, IProjectSubscriptionUpdate>>.Create((Tuple.Create(IProjectTreeSnapshotFactory.Create(tree), projectUpdate))));

            // If fileToWatch is null then we expect to not register any filewatcher.
            var times = fileToWatch == null ? Times.Never() : Times.Once();
            Mock.Get(fileChangeService).Verify(s => s.AdviseFileChange(fileToWatch ?? It.IsAny<string>(), It.IsAny<uint>(), watcher, out adviseCookie), times);
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

        public async Task VerifyFileWatcherRegistrationOnTreeChange(string inputTree, string changedTree, int numRegisterCalls, int numUnregisterCalls)
        {
            var spMock = new IAsyncServiceProviderMoq();
            uint adviseCookie = 100;
            var fileChangeService = IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(adviseCookie);
            spMock.AddService(typeof(SVsFileChangeEx), fileChangeService);
            var tasksService = IUnconfiguredProjectTasksServiceFactory.ImplementLoadedProjectAsync<ConfiguredProject>(t => t());

            var watcher = new ProjectAssetFileWatcher(spMock,
                                                     IProjectTreeProviderFactory.Create(),
                                                     IUnconfiguredProjectCommonServicesFactory.Create(threadingService: IProjectThreadingServiceFactory.Create()),
                                                     tasksService,
                                                     IActiveConfiguredProjectSubscriptionServiceFactory.Create());
            watcher.Load();
            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(ProjectCurrentStateJson);

            var firstTree = ProjectTreeParser.Parse(inputTree);
            await watcher.DataFlow_ChangedAsync(IProjectVersionedValueFactory<Tuple<IProjectTreeSnapshot, IProjectSubscriptionUpdate>>.Create((Tuple.Create(IProjectTreeSnapshotFactory.Create(firstTree), projectUpdate))));

            var secondTree = ProjectTreeParser.Parse(changedTree);
            await watcher.DataFlow_ChangedAsync(IProjectVersionedValueFactory<Tuple<IProjectTreeSnapshot, IProjectSubscriptionUpdate>>.Create((Tuple.Create(IProjectTreeSnapshotFactory.Create(secondTree), projectUpdate))));

            // If fileToWatch is null then we expect to not register any filewatcher.
            var fileChangeServiceMock = Mock.Get(fileChangeService);
            fileChangeServiceMock.Verify(s => s.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), watcher, out adviseCookie),
                                         Times.Exactly(numRegisterCalls));
            fileChangeServiceMock.Verify(s => s.UnadviseFileChange(adviseCookie), Times.Exactly(numUnregisterCalls));
        }

        [Fact]
        public async Task WhenBaseIntermediateOutputPathNotSet_DoesNotAttemptToAdviseFileChange()
        {
            var spMock = new IAsyncServiceProviderMoq();
            var fileChangeService = IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100);
            spMock.AddService(typeof(SVsFileChangeEx), fileChangeService);
            var tasksService = IUnconfiguredProjectTasksServiceFactory.ImplementLoadedProjectAsync<ConfiguredProject>(t => t());

            var watcher = new ProjectAssetFileWatcher(spMock,
                                                     IProjectTreeProviderFactory.Create(),
                                                     IUnconfiguredProjectCommonServicesFactory.Create(threadingService: IProjectThreadingServiceFactory.Create()),
                                                     tasksService,
                                                     IActiveConfiguredProjectSubscriptionServiceFactory.Create());

            var tree = ProjectTreeParser.Parse(@"Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\foo.proj""");
            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(@"{
    ""CurrentState"": {
        ""ConfigurationGeneral"": {
            ""Properties"": {
               ""MSBuildProjectFullPath"": ""C:\\Foo\\foo.proj""
            }
        }
    }
}");

            watcher.Load();
            await watcher.DataFlow_ChangedAsync(IProjectVersionedValueFactory<Tuple<IProjectTreeSnapshot, IProjectSubscriptionUpdate>>.Create((Tuple.Create(IProjectTreeSnapshotFactory.Create(tree), projectUpdate))));

            var fileChangeServiceMock = Mock.Get(fileChangeService);
            uint cookie;
            fileChangeServiceMock.Verify(s => s.AdviseFileChange(It.IsAny<string>(), It.IsAny<uint>(), watcher, out cookie),
                                         Times.Never());
        }

        private PropertyPageData CreateBaseIntermediateOutputPathProperty(object baseIntermediateOutputPath)
        {
            return new PropertyPageData
            {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.BaseIntermediateOutputPathProperty,
                Value = baseIntermediateOutputPath
            };
        }
    }
}
