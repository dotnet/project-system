// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    public sealed class IDependencyModelEqualityComparerTests
    {
        [Fact]
        public void EqualsAndGetHashCode()
        {
            var model1 = new TestDependencyModel
            {
                Id = "id1",
                ProviderType = "provider1"
            };

            var model2 = new TestDependencyModel
            {
                Id = "id1",
                ProviderType = "provider1"
            };

            var model3 = new TestDependencyModel
            {
                Id = "DIFFERENT",
                ProviderType = "provider1"
            };

            var model4 = new TestDependencyModel
            {
                Id = "id1",
                ProviderType = "DIFFERENT"
            };

            Assert.True(IDependencyModelEqualityComparer.Instance.Equals(model1, model2));
            Assert.True(IDependencyModelEqualityComparer.Instance.Equals(model2, model1));

            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model1, model3));
            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model3, model1));

            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model1, model4));
            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model4, model1));

            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model3, model4));
            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model4, model3));

            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(model1, null));
            Assert.False(IDependencyModelEqualityComparer.Instance.Equals(null, model1));

            Assert.True(IDependencyModelEqualityComparer.Instance.Equals(null, null));

            Assert.Equal(
                IDependencyModelEqualityComparer.Instance.GetHashCode(model1),
                IDependencyModelEqualityComparer.Instance.GetHashCode(model2));

            Assert.NotEqual(
                IDependencyModelEqualityComparer.Instance.GetHashCode(model1),
                IDependencyModelEqualityComparer.Instance.GetHashCode(model3));

            Assert.NotEqual(
                IDependencyModelEqualityComparer.Instance.GetHashCode(model1),
                IDependencyModelEqualityComparer.Instance.GetHashCode(model4));

            Assert.NotEqual(
                IDependencyModelEqualityComparer.Instance.GetHashCode(model3),
                IDependencyModelEqualityComparer.Instance.GetHashCode(model4));
        }
    }
}
