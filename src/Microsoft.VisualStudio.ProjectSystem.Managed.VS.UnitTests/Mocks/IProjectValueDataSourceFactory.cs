// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class IProjectValueDataSourceFactory
    {
        public static IProjectValueDataSource<T> CreateInstance<T>()
        {
            return Mock.Of<IProjectValueDataSource<T>>();
        }
    }
}
