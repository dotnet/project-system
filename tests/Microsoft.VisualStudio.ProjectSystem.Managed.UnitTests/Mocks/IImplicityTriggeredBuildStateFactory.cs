// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Managed.Build
{
    internal class IImplicityTriggeredBuildStateFactory
    {
        public static IImplicitlyTriggeredBuildState Create(bool implicitBuild)
        {
            var mock = new Mock<IImplicitlyTriggeredBuildState>();

            mock.SetupGet(state => state.IsImplicitlyTriggeredBuild).Returns(implicitBuild);

            return mock.Object;
        }
    }
}
