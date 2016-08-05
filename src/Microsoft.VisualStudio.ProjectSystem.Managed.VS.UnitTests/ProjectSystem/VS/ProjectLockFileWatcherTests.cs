// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class ProjectLockFileWatcherTests
    {
        [Theory]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\""
    project.json, FilePath: ""C:\Foo\project.json""", @"C:\Foo\project.lock.json")]
        [InlineData(@"
Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\""", null)]
        public void VerifyFileWatcherRegistration(string inputTree, string fileToWatch)
        {
            var spMock = new IServiceProviderMoq();
            var fileChangeService = IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100);
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), fileChangeService);

            var watcher = new ProjectLockFileWatcher(spMock,
                                                     IProjectTreeProviderFactory.Create(),
                                                     IUnconfiguredProjectCommonServicesFactory.Create(IUnconfiguredProjectFactory.Create(filePath:@"C:\Foo\foo.proj")),
                                                     IProjectLockServiceFactory.Create());

            var tree = ProjectTreeParser.Parse(inputTree);
            watcher.ProjectTree_ChangedAsync(IProjectVersionedValueFactory<IProjectTreeSnapshot>.Create(IProjectTreeSnapshotFactory.Create(tree)));

            uint adviseCookie = 100;
            var times = fileToWatch == null ? Times.Never() : Times.Once();
            Mock.Get<IVsFileChangeEx>(fileChangeService).Verify(s => s.AdviseFileChange(fileToWatch ?? It.IsAny<string>(), It.IsAny<uint>(), watcher, out adviseCookie), times);
        }
    }
}
