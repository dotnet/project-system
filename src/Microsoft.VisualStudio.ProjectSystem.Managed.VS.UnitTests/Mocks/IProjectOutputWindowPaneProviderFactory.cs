// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Logging
{
    internal static class IProjectOutputWindowPaneProviderFactory
    {
        public static IProjectOutputWindowPaneProvider Create()
        {
            return Mock.Of<IProjectOutputWindowPaneProvider>();
        }
    }
}