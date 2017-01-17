// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsSolutionFactory
    {
        public static IVsSolution CreateWithSolutionDirectory(FuncWithOutThreeArgs<string, string, string, int> func)
        {
            var mock = new Mock<IVsSolution>();
            string directory;
            string solutionFile;
            string userSettings;
            mock.Setup(x => x.GetSolutionInfo(out directory, out solutionFile, out userSettings)).Returns(func);
            return mock.Object;
        }

        public static IVsSolution CreateWithAdviseUnadviseSolutionEvents(uint adviseCookie)
        {
            var mock = new Mock<IVsSolution>();
            mock.Setup(x => x.AdviseSolutionEvents(It.IsAny<IVsSolutionEvents>(), out adviseCookie)).Returns(VSConstants.S_OK);
            mock.Setup(x => x.UnadviseSolutionEvents(It.IsAny<uint>())).Returns(VSConstants.S_OK);
            return mock.Object;
        }
    }
}
