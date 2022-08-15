// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class ICommandLineParserServiceFactory
    {
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
