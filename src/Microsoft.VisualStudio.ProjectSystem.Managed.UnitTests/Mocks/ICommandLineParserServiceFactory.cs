// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class ICommandLineParserServiceFactory
    {
        public static ICommandLineParserService Create()
        {
            return Mock.Of<ICommandLineParserService>();
        }

        public static ICommandLineParserService ImplementParse(Func<IEnumerable<string>, string, BuildOptions> action)
        {
            var mock = new Mock<ICommandLineParserService>();

            mock.Setup(s => s.Parse(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .Returns(action);

            return mock.Object;
        }

        public static ICommandLineParserService CreateCSharp()
        {
            return ImplementParse((arguments, baseDirectory) =>
            {
                return BuildOptions.FromCommandLineArguments(
                    CSharpCommandLineParser.Default.Parse(arguments, baseDirectory, sdkDirectory: null, additionalReferenceDirectories: null));
            });
        }
    }
}
