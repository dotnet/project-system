// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class ICommandLineHandlerFactory
    {
        public static ICommandLineHandler ImplementHandle(Action<IComparable, BuildOptions, BuildOptions, bool, IProjectLogger> action)
        {
            var mock = new Mock<ICommandLineHandler>();

            mock.Setup(h => h.Handle(It.IsAny<IComparable>(), It.IsAny<BuildOptions>(), It.IsAny<BuildOptions>(), It.IsAny<bool>(), It.IsAny<IProjectLogger>()))
                .Callback(action);

            return mock.Object;
        }
    }
}
