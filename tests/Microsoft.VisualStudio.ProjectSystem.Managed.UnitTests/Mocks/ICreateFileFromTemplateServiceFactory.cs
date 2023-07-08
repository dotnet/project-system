// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ICreateFileFromTemplateServiceFactory
    {
        public static ICreateFileFromTemplateService Create()
        {
            var mock = new Mock<ICreateFileFromTemplateService>();

            mock.Setup(s => s.CreateFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((templateFile, path) =>
                {
                    return TaskResult.True;
                });

            return mock.Object;
        }
    }
}
