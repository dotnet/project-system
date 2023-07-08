// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading.Tasks;
using Xunit.Sdk;

// Nullable annotations don't add a lot of value to this class, and until https://github.com/dotnet/roslyn/issues/33199 is fixed
// MemberData doesn't work anyway
#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    public class DesignTimeInputsFileWatcherTests
    {
        private const int TestTimeoutMillisecondsDelay = 1000;

        public static IEnumerable<object[]> GetTestCases()
        {
            return new[]
            {
                // A single design time input
                new object[]
                {
                    new string[] { "File1.cs" },              // design time inputs
                    new string[] { },                         // shared design time inputs
                    new string[] { "File1.cs" },              // expected watched files
                    new string[] { },                         // file change notifications to send
                    new string[] { }                          // file change notifications expected
                },

                // A design time input and a shared design time input
                new object[]
                {
                    new string[] { "File1.cs" },
                    new string[] { "File2.cs" },
                    new string[] { "File1.cs", "File2.cs" },
                    new string[] { },
                    new string[] { }
                },

                // A file that is both design time and shared, should only be watched once
                new object[]
                {
                    new string[] { "File1.cs" },
                    new string[] { "File2.cs", "File1.cs" },
                    new string[] { "File1.cs", "File2.cs" },
                    new string[] { },
                    new string[] { }
                },

                // A design time input that gets modified
                 new object[]
                {
                    new string[] { "File1.cs" },
                    new string[] { },
                    new string[] { "File1.cs" },
                    new string[] { "File1.cs", "File1.cs", "File1.cs" },
                    new string[] { "File1.cs", "File1.cs", "File1.cs" }
                },

                // A design time input and a shared design time input, that both change, to ensure ordering is correct
                new object[]
                {
                    new string[] { "File1.cs" },
                    new string[] { "File2.cs" },
                    new string[] { "File1.cs", "File2.cs" },
                    new string[] { "File1.cs", "File2.cs" },
                    new string[] { "File1.cs", "File2.cs" }
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetTestCases))]
        internal async Task VerifyDesignTimeInputsWatched(string[] designTimeInputs, string[] sharedDesignTimeInputs, string[] watchedFiles, string[] fileChangeNotificationsToSend, string[] fileChangeNotificationsExpected)
        {
            var fileChangeService = new IVsAsyncFileChangeExMock();

            using DesignTimeInputsFileWatcher watcher = CreateDesignTimeInputsFileWatcher(fileChangeService, out ProjectValueDataSource<DesignTimeInputs> source);

            watcher.AllowSourceBlockCompletion = true;

            // Send our input. DesignTimeInputs expects full file paths
            await source.SendAsync(new DesignTimeInputs(designTimeInputs.Select(f => f), sharedDesignTimeInputs.Select(f => f)));

            // The TaskCompletionSource is the thing we use to wait for the test to finish
            var finished = new TaskCompletionSource();

            int notificationCount = 0;
            // Create a block to receive the output
            var receiver = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<string[]>>(val =>
            {
                foreach (string file in val.Value)
                {
                    Assert.Equal(fileChangeNotificationsExpected[notificationCount++], file);
                }

                // if we've seen every file, we're done
                if (notificationCount == fileChangeNotificationsExpected.Length)
                {
                    finished.SetResult();
                }
            });
            watcher.SourceBlock.LinkTo(receiver, DataflowOption.PropagateCompletion);

            // Send down our fake file changes
            watcher.FilesChanged((uint)fileChangeNotificationsToSend.Length, fileChangeNotificationsToSend, null);

            source.SourceBlock.Complete();

            await source.SourceBlock.Completion;

            watcher.SourceBlock.Complete();

            await watcher.SourceBlock.Completion;

            // The timeout here is annoying, but even though our test is "smart" and waits for data, unfortunately if the code breaks the test is more likely to hang than fail
            if (await Task.WhenAny(finished.Task, Task.Delay(TestTimeoutMillisecondsDelay)) != finished.Task)
            {
                throw new AssertActualExpectedException(fileChangeNotificationsExpected.Length, notificationCount, $"Timed out after {TestTimeoutMillisecondsDelay}ms");
            }

            // Observe the task in case of exceptions
            await finished.Task;

            // Dispose the watcher so that internal blocks complete (especially for tests that don't send any file changes)
            await watcher.DisposeAsync();

            // Make sure we watched all of the files we should
            Assert.Equal(watchedFiles, fileChangeService.UniqueFilesWatched);

            // Should clean up and unwatch everything
            Assert.Empty(fileChangeService.WatchedFiles.ToArray());
        }

        private static DesignTimeInputsFileWatcher CreateDesignTimeInputsFileWatcher(IVsAsyncFileChangeEx fileChangeService, out ProjectValueDataSource<DesignTimeInputs> source)
        {
            // Create our mock design time inputs data source, but with a source we can actually use
            var services = IProjectCommonServicesFactory.CreateWithDefaultThreadingPolicy();
            source = ProjectValueDataSourceFactory.Create<DesignTimeInputs>(services);

            var mock = new Mock<IDesignTimeInputsDataSource>();
            mock.SetupGet(s => s.SourceBlock)
                .Returns(source.SourceBlock);

            var dataSource = mock.Object;

            var threadingService = IProjectThreadingServiceFactory.Create();
            var unconfiguredProject = UnconfiguredProjectFactory.Create(fullPath: @"C:\MyProject\MyProject.csproj");
            var unconfiguredProjectServices = IUnconfiguredProjectServicesFactory.Create(
                    projectService: IProjectServiceFactory.Create(
                        services: ProjectServicesFactory.Create(
                            threadingService: threadingService)));

            // Create our class under test
            return new DesignTimeInputsFileWatcher(unconfiguredProject, unconfiguredProjectServices, threadingService, dataSource, IVsServiceFactory.Create<SVsFileChangeEx, IVsAsyncFileChangeEx>(fileChangeService));
        }
    }
}
