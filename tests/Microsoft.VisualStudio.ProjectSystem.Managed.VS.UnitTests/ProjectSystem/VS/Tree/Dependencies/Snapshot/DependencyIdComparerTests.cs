// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    public class DependencyIdComparerTests
    {
        [Fact]
        public void EqualsAndGetHashCode()
        {
            var comparer = DependencyIdComparer.Instance;

            var dependencyModel1 = new TestDependencyModel { ProviderType = "providerType", Id = "someId1" };
            var dependencyModel2 = new TestDependencyModel { ProviderType = "providerType", Id = "someId1" };
            var dependencyModel3 = new TestDependencyModel { ProviderType = "providerType", Id = "someId_other" };

            var targetFramework = new TargetFramework("tfm1");
            var dependency1 = new Dependency(dependencyModel1, targetFramework, @"C:\Foo\Project.csproj");
            var dependency2 = new Dependency(dependencyModel2, targetFramework, @"C:\Foo\Project.csproj");
            var dependency3 = new Dependency(dependencyModel3, targetFramework, @"C:\Foo\Project.csproj");

            Assert.Equal(dependency1, dependency2, comparer);
            Assert.NotEqual(dependency1, dependency3, comparer);
            Assert.False(comparer.Equals(dependency1, null!));
            Assert.False(comparer.Equals(null!, dependency1!));
            Assert.Equal(comparer.GetHashCode(dependency1!), comparer.GetHashCode(dependency2));
            Assert.NotEqual(comparer.GetHashCode(dependency1!), comparer.GetHashCode(dependency3));
        }
    }
}
