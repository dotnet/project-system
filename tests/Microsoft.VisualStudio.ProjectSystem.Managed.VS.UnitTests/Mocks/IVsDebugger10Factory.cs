// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsDebugger10Factory
    {
        public static IVsDebugger10 ImplementIsIntegratedConsoleEnabled(bool enabled)
        {
            var mock = new Mock<IVsDebugger10>();
            mock.Setup(d => d.IsIntegratedConsoleEnabled(out enabled));

            return mock.Object;
        }
    }
}
