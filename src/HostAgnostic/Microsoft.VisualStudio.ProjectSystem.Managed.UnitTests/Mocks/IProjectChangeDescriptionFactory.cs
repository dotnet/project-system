// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectChangeDescriptionFactory
    {
        public static IProjectChangeDescription Create()
        {
            return Mock.Of<IProjectChangeDescription>();
        }
    }
}
