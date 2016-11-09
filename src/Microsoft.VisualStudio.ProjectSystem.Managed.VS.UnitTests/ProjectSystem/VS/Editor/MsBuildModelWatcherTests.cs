using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Moq;
using Xunit;
using HandlerCallback = System.EventHandler<Microsoft.Build.Evaluation.ProjectXmlChangedEventArgs>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [ProjectSystemTrait]
    public class MsBuildModelWatcherTests
    {
        [Fact]
        public void MsBuildModelWatcher_InitializeAsync_SetsUpSubscription()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var fileSystem = new IFileSystemMock();
            HandlerCallback subscription = null;
            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXmlAndXmlChangedEvents("", newSub => subscription = newSub,
                sub => Assert.False(true, "Should not have called Unsubscribe in this test, as dispose isn't called."));
            var project = IUnconfiguredProjectFactory.Create();

            var watcher = new MsBuildModelWatcher(threadingService, fileSystem, msbuildAccessor, project);

            watcher.Initialize(@"C:\Test\Test.proj");

            Assert.NotNull(subscription);
            Assert.Equal(watcher.ProjectXmlHandler, subscription);
            Mock.Get(msbuildAccessor).Verify(m => m.SubscribeProjectXmlChangedEventAsync(project, subscription), Times.Once);
            Assert.Equal(0, fileSystem.Files.Count);
        }

        [Fact]
        public void MsBuildModelWatcher_HandlerCalled_WritesNewXmlToDisk()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var fileSystem = new IFileSystemMock();
            HandlerCallback subscription = null;
            var xml = @"<Project></Project>";
            var file = @"C:\Test\Test.proj";
            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXmlAndXmlChangedEvents(xml, newSub => subscription = newSub,
                sub => Assert.False(true, "Should not have called Unsubscribe in this test, as dispose isn't called."));
            var project = IUnconfiguredProjectFactory.Create();

            var watcher = new MsBuildModelWatcher(threadingService, fileSystem, msbuildAccessor, project);
            watcher.Initialize(file);

            Assert.NotNull(subscription);
            watcher.XmlHandler(xml);
            Assert.True(fileSystem.FileExists(file));
            Assert.Equal(xml, fileSystem.ReadAllText(file));
        }

        [Fact]
        public void MsBuildModelWatcher_Dispose_CallsUnsubscribe()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var fileSystem = new IFileSystemMock();
            HandlerCallback subscription = null;
            HandlerCallback unsubscription = null;
            var xml = @"<Project></Project>";
            var file = @"C:\Test\Test.proj";
            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXmlAndXmlChangedEvents(xml, newSub => subscription = newSub,
                sub => unsubscription = sub);
            var project = IUnconfiguredProjectFactory.Create();

            var watcher = new MsBuildModelWatcher(threadingService, fileSystem, msbuildAccessor, project);
            watcher.Initialize(file);

            Assert.NotNull(subscription);
            watcher.XmlHandler(xml);
            Assert.True(fileSystem.FileExists(file));
            Assert.Equal(xml, fileSystem.ReadAllText(file));

            watcher.Dispose();
            Assert.NotNull(unsubscription);
            Assert.Equal(subscription, unsubscription);

            // Ensure multiple calls to dispose don't cause multiple calls to unsubscribe
            watcher.Dispose();
            Mock.Get(msbuildAccessor).Verify(m => m.UnsubscribeProjectXmlChangedEventAsync(project, unsubscription), Times.Once);
        }
    }
}
