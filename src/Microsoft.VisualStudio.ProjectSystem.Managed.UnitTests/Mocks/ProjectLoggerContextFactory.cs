// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    internal static class ProjectLoggerContextFactory
    {
        public static ProjectLoggerContext Create()
        {
            var mock = new Mock<IProjectLogger>();
            return mock.Object.BeginContext("");
        }
    }
}
