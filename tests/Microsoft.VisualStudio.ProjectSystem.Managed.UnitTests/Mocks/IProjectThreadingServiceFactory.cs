// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static partial class IProjectThreadingServiceFactory
    {
        public static IProjectThreadingService Create(bool verifyOnUIThread = true)
        {
            return new ProjectThreadingService(verifyOnUIThread);
        }

        public static IProjectThreadingService ImplementVerifyOnUIThread(Action action)
        {
            var mock = new Mock<IProjectThreadingService>();

            mock.Setup(s => s.VerifyOnUIThread())
                .Callback(action);

            return mock.Object;
        }
    }
}
