// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Logging
{
    public class ProjectOutputWindowPaneProviderTests
    {
        [Fact]
        public async Task GetOutputWindowPaneAsync_WhenNoIVsOutputWindow_ReturnsNull()
        {
            var provider = CreateInstance();

            var result = await provider.GetOutputWindowPaneAsync();

            Assert.Null(result);
        }

        [Fact]
        public async Task GetOutputWindowPaneAsync_ReturnsResult()
        {
            var outputWindow = IVsOutputWindowFactory.Create();

            var provider = CreateInstance(outputWindow: outputWindow);

            var result = await provider.GetOutputWindowPaneAsync();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetOutputWindowPaneAsync_ReturnsCachedResult()
        {
            var outputWindow = IVsOutputWindowFactory.Create();

            var provider = CreateInstance(outputWindow: outputWindow);

            var result1 = await provider.GetOutputWindowPaneAsync();
            var result2 = await provider.GetOutputWindowPaneAsync();

            Assert.Same(result1, result2);
        }

        [Fact]
        public async Task GetOutputWindowPaneAsync_ReactivatesPreviouslyActiveWindow()
        {
            int callCount = 0;
            var pane = IVsOutputWindowPaneFactory.ImplementActivate(() => { callCount++; return 0; });

            var outputWindow = IVsOutputWindow2Factory.CreateWithActivePane(pane);
            var provider = CreateInstance(outputWindow: outputWindow);

            await provider.GetOutputWindowPaneAsync();

            Assert.Equal(1, callCount);
        }

        private static ProjectOutputWindowPaneProvider CreateInstance(IProjectThreadingService? threadingService = null, IVsOutputWindow? outputWindow = null)
        {
            threadingService ??= IProjectThreadingServiceFactory.Create();

            var outputWindowService = IVsUIServiceFactory.Create<SVsOutputWindow, IVsOutputWindow?>(outputWindow);

            return new ProjectOutputWindowPaneProvider(threadingService, outputWindowService);
        }
    }
}
