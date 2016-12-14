// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ICreateFileFromTemplateServiceFactory
    {
        public static ICreateFileFromTemplateService Create()
        {
            var mock = new Mock<ICreateFileFromTemplateService>();

            mock.Setup(s => s.CreateFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string, string>((templateFile, parentNode, specialFileName) =>
                {
                    return Task.FromResult(true);
                });

            return mock.Object;
        }
    }
}
