// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Moq;
using System.Threading.Tasks;
using Xunit;
using HandlerCallback = System.EventHandler<Microsoft.Build.Evaluation.ProjectXmlChangedEventArgs>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [ProjectSystemTrait]
    public class MsBuildModelWatcherTests
    {
        [Fact]
        public async Task MsBuildModelWatcher_InitializeAsync_SetsUpSubscription()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var fileSystem = new IFileSystemMock();
            HandlerCallback subscription = null;
            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXmlAndXmlChangedEvents("", newSub => subscription = newSub,
                sub => Assert.False(true, "Should not have called Unsubscribe in this test, as dispose isn't called."));
            var project = IUnconfiguredProjectFactory.Create();

            var watcher = new MsBuildModelWatcher(threadingService, fileSystem, msbuildAccessor, project);

            await watcher.InitializeAsync(@"C:\Test\Test.proj", "");

            Assert.NotNull(subscription);
            Assert.Equal(watcher.ProjectXmlHandler, subscription);
            Mock.Get(msbuildAccessor).Verify(m => m.SubscribeProjectXmlChangedEventAsync(project, subscription), Times.Once);
            Assert.Equal(0, fileSystem.Files.Count);
        }

        [Fact]
        public async Task MsBuildModelWatcher_HandlerCalled_WritesNewXmlToDisk()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var fileSystem = new IFileSystemMock();
            HandlerCallback subscription = null;
            var xml = @"<Project></Project>";
            var file = @"C:\Test\Test.proj";
            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXmlAndXmlChangedEvents(xml, newSub => subscription = newSub,
                sub => Assert.False(true, "Should not have called Unsubscribe in this test, as dispose isn't called."));
            var project = IUnconfiguredProjectFactory.Create(filePath: file);

            var watcher = new MsBuildModelWatcher(threadingService, fileSystem, msbuildAccessor, project);
            await watcher.InitializeAsync(file, "");

            Assert.NotNull(subscription);
            watcher.XmlHandler(xml, file);
            Assert.True(fileSystem.FileExists(file));
            Assert.Equal(xml, fileSystem.ReadAllText(file));
        }

        [Fact]
        public async Task MsBuildModelWatcher_Dispose_CallsUnsubscribe()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var fileSystem = new IFileSystemMock();
            HandlerCallback subscription = null;
            HandlerCallback unsubscription = null;
            var xml = @"<Project></Project>";
            var file = @"C:\Test\Test.proj";
            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXmlAndXmlChangedEvents(xml, newSub => subscription = newSub,
                sub => unsubscription = sub);
            var project = IUnconfiguredProjectFactory.Create(filePath: file);

            var watcher = new MsBuildModelWatcher(threadingService, fileSystem, msbuildAccessor, project);
            await watcher.InitializeAsync(file, "");

            Assert.NotNull(subscription);
            watcher.XmlHandler(xml, file);
            Assert.True(fileSystem.FileExists(file));
            Assert.Equal(xml, fileSystem.ReadAllText(file));

            watcher.Dispose();
            Assert.NotNull(unsubscription);
            Assert.Equal(subscription, unsubscription);

            // Ensure multiple calls to dispose don't cause multiple calls to unsubscribe
            watcher.Dispose();
            Mock.Get(msbuildAccessor).Verify(m => m.UnsubscribeProjectXmlChangedEventAsync(project, unsubscription), Times.Once);
        }

        [Fact]
        public async Task MsBuildModelWatcher_SameXml_DoesNotWriteMultipleTimes()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var fileSystem = new IFileSystemMock();
            HandlerCallback subscription = null;
            var xml = @"<Project></Project>";
            var file = @"C:\Test\Test.proj";
            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXmlAndXmlChangedEvents(xml, newSub => subscription = newSub,
                sub => Assert.False(true, "Should not have called Unsubscribe in this test, as dispose isn't called."));
            var project = IUnconfiguredProjectFactory.Create(filePath: file);

            var watcher = new MsBuildModelWatcher(threadingService, fileSystem, msbuildAccessor, project);
            await watcher.InitializeAsync(file, "");

            Assert.NotNull(subscription);
            watcher.XmlHandler(xml, file);
            Assert.True(fileSystem.FileExists(file));
            Assert.Equal(xml, fileSystem.ReadAllText(file));

            // If we delete the underlying xml from the file system, a consectutive call to XmlHandler with the same xml should not regenerate
            // it.
            fileSystem.RemoveFile(file);
            watcher.XmlHandler(xml, file);
            Assert.False(fileSystem.FileExists(file));
        }

        [Fact]
        public async Task MsBuildModelWatcher_DifferentFile_DoesNotWriteToDisk()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var fileSystem = new IFileSystemMock();
            HandlerCallback subscription = null;
            var xml = @"<Project></Project>";
            var file = @"C:\Test\Test.proj";
            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXmlAndXmlChangedEvents(xml, newSub => subscription = newSub,
                sub => Assert.False(true, "Should not have called Unsubscribe in this test, as dispose isn't called."));
            var project = IUnconfiguredProjectFactory.Create(filePath: file);

            var watcher = new MsBuildModelWatcher(threadingService, fileSystem, msbuildAccessor, project);
            await watcher.InitializeAsync(file, "");

            Assert.NotNull(subscription);
            watcher.XmlHandler(xml, @"C:\Test\AnotherTest.proj");
            Assert.False(fileSystem.FileExists(file));
        }
    }
}
