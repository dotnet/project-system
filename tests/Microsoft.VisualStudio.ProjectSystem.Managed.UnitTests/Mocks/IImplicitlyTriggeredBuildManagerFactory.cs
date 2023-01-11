// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable CS0618 // Type or member is obsolete - IImplicitlyTriggeredBuildManager is marked obsolete as it may eventually be replaced with a different API.

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    internal static class IImplicitlyTriggeredBuildManagerFactory
    {
        public static IImplicitlyTriggeredBuildManager Create(
            Action? onImplicitBuildStart = null,
            Action? onImplicitBuildEndOrCancel = null,
            Action<ImmutableArray<string>>? onImplictBuildStartWithStartupPaths = null)
        {
            var mock = new Mock<IImplicitlyTriggeredBuildManager2>();

            if (onImplicitBuildStart is not null)
            {
                mock.Setup(t => t.OnBuildStart())
                    .Callback(onImplicitBuildStart);
            }

            if (onImplicitBuildEndOrCancel is not null)
            {
                mock.Setup(t => t.OnBuildEndOrCancel())
                    .Callback(onImplicitBuildEndOrCancel);
            }

            if (onImplictBuildStartWithStartupPaths is not null)
            {
                mock.Setup(t => t.OnBuildStart(It.IsAny<ImmutableArray<string>>()))
                    .Callback(onImplictBuildStartWithStartupPaths);
            }

            return mock.Object;
        }
    }
}
