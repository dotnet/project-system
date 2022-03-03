// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class ICommandLineHandlerFactory
    {
        public static ICommandLineHandler ImplementHandle(Action<IComparable, BuildOptions, BuildOptions, ContextState, IProjectDiagnosticOutputService> action)
        {
            var mock = new Mock<ICommandLineHandler>();

            mock.Setup(h => h.Handle(It.IsAny<IComparable>(), It.IsAny<BuildOptions>(), It.IsAny<BuildOptions>(), It.IsAny<ContextState>(), It.IsAny<IProjectDiagnosticOutputService>()))
                .Callback(action);

            return mock.Object;
        }
    }
}
