// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsSolutionFactory
    {
        public static FuncWithOut<IVsSolutionEvents, uint, int> DefaultAdviseCallback => (IVsSolutionEvents events, out uint cookie) =>
        {
            cookie = 0;
            return VSConstants.S_OK;
        };

        public static Func<uint, int> DefaultUnadviseCallback => (uint cookie) => VSConstants.S_OK;

        public static IVsSolution Create() => Mock.Of<IVsSolution>();

        public static IVsSolution CreateWithSolutionDirectory(FuncWithOutThreeArgs<string, string, string, int> func)
        {
            var mock = new Mock<IVsSolution>();
            string directory;
            string solutionFile;
            string userSettings;
            mock.Setup(x => x.GetSolutionInfo(out directory, out solutionFile, out userSettings)).Returns(func);
            return mock.Object;
        }

        public static IVsSolution CreateWithAdviseUnadviseSolutionEvents(uint adviseCookie, bool? isFullyLoaded = null)
        {
            var mock = new Mock<IVsSolution>();
            mock.Setup(x => x.AdviseSolutionEvents(It.IsAny<IVsSolutionEvents>(), out adviseCookie)).Returns(VSConstants.S_OK);
            mock.Setup(x => x.UnadviseSolutionEvents(It.IsAny<uint>())).Returns(VSConstants.S_OK);
            if (isFullyLoaded != null)
            {
                object value = isFullyLoaded.Value;
                mock.Setup(x => x.GetProperty(It.IsAny<int>(), out value)).Returns(VSConstants.S_OK);
            }

            return mock.Object;
        }

        public static IVsSolution Implement(FuncWithOut<IVsSolutionEvents, uint, int> adviseCallback,
            Func<uint, int> unadviseCallback,
            FuncWithOutThreeArgs<string, string, string, int> solutionInfoCallback)
        {
            var mock = new Mock<IVsSolution>();
            uint cookie;
            string directory;
            string solutionFile;
            string userSettings;

            mock.Setup(x => x.AdviseSolutionEvents(It.IsAny<IVsSolutionEvents>(), out cookie)).Returns(adviseCallback);
            mock.Setup(x => x.UnadviseSolutionEvents(It.IsAny<uint>())).Returns(unadviseCallback);
            mock.Setup(x => x.GetSolutionInfo(out directory, out solutionFile, out userSettings)).Returns(solutionInfoCallback);
            return mock.Object;
        }
    }
}
