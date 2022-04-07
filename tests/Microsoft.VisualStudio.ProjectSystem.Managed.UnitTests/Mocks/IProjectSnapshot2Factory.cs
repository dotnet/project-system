// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectSnapshot2Factory
    {
        public static IProjectSnapshot2 Create(
            IImmutableDictionary<string, DateTime>? dependentFileTimes = null)
        {
            var mock = new Mock<IProjectSnapshot2>();

            mock.Setup(s => s.AdditionalDependentFileTimes)
                .Returns(dependentFileTimes ?? ImmutableDictionary<string, DateTime>.Empty);

            return mock.Object;
        }

        public static IProjectSnapshot2 WithAdditionalDependentFileTime(string path, DateTime fileTime)
        {
            var fileTimes = ImmutableDictionary<string, DateTime>.Empty.Add(path, fileTime);

            var mock = new Mock<IProjectSnapshot2>();
            mock.Setup(s => s.AdditionalDependentFileTimes)
                .Returns(fileTimes);

            return mock.Object;
        }
    }
}
