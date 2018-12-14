// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public sealed class UnconfiguredProjectExtensionsTests
    {
        [Theory]
        [InlineData(@"C:\Project\MyProject.csproj", @"C:\Project\Foo.cs",           @"Foo.cs")]
        [InlineData(@"C:\Project\MyProject.csproj", @"D:\Other\Foo.cs",             @"D:\Other\Foo.cs")]
        [InlineData(@"C:\Project\MyProject.csproj", @"C:\Project\MyProject.csproj", @"MyProject.csproj")]
        public void GetRelativePath(string projectPath, string path, string expectedRelative)
        {
            var project = UnconfiguredProjectFactory.ImplementFullPath(projectPath);

            Assert.Equal(expectedRelative, project.GetRelativePath(path));
        }
    }
}
