// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

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
