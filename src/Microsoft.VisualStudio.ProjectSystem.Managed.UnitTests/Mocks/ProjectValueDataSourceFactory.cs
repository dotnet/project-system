// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ProjectValueDataSourceFactory
    {
        public static ProjectValueDataSource<T> Create<T>(IProjectCommonServices services)
            where T : class
        {
            return new ProjectValueDataSource<T>(services);
        }
    }
}
