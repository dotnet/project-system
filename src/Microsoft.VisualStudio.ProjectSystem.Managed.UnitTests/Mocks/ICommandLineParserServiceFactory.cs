// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal static class ICommandLineParserServiceFactory
    {
        public static ICommandLineParserService Create()
        {
            return CreateFromOptions(new BuildOptions(
                Enumerable.Empty<CommandLineSourceFile>(),
                Enumerable.Empty<CommandLineSourceFile>(),
                Enumerable.Empty<CommandLineReference>(),
                Enumerable.Empty<CommandLineAnalyzerReference>()));
        }

        public static ICommandLineParserService CreateFromOptions(BuildOptions options)
        {
            var context = new Mock<ICommandLineParserService>();

            context.Setup(c => c.Parse(It.IsAny<IEnumerable<string>>())).Returns(options);

            return context.Object;
        }
    }
}
