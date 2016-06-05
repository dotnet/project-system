// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal static class ICreateFileFromTemplateServiceFactory
    {
        public static ICreateFileFromTemplateService Create()
        {
            var mock = new Mock<ICreateFileFromTemplateService>();

            mock.Setup(s => s.CreateFileAsync(It.IsAny<string>(), It.IsAny<IProjectTree>(), It.IsAny<string>()))
                .Returns<string, IProjectTree, string>((templateFile, parentNode, specialFileName) =>
                {
                    var newNode = ProjectTreeParser.Parse($@"{specialFileName}, FilePath: ""{Path.Combine(parentNode.FilePath, specialFileName)}""");
                    parentNode.Add(newNode);
                    return Task.FromResult(true);
                });

            return mock.Object;
        }
    }
}
