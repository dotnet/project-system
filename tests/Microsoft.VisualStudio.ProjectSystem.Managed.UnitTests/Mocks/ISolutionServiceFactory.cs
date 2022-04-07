// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ISolutionServiceFactory
    {
        public static ISolutionService Create()
        {
            return ImplementSolutionLoadedInHost(() => new Task(() => { }));
        }

        public static ISolutionService ImplementSolutionLoadedInHost(Func<Task> action)
        {
            var mock = new Mock<ISolutionService>();
            mock.Setup(m => m.LoadedInHost)
                .Returns(action);

            return mock.Object;
        }
    }
}
