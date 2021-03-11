// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectDependentFileChangeNotificationServiceFactory
    {
        public static IProjectDependentFileChangeNotificationService Create()
        {
            var mock = new Mock<IProjectDependentFileChangeNotificationService>();

            mock.Setup(s => s.OnAfterDependentFilesChanged(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<UnconfiguredProject?>()));

            return mock.Object;
        }
    }
}
