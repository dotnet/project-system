// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.References
{
    public class AlwaysAllowValidProjectReferenceCheckerTests
    {
        [Fact]
        public void CanAddProjectReferenceAsync_NullAsReferencedProject_ThrowsArgumentNull()
        {
            var checker = CreateInstance();

            Assert.ThrowsAsync<ArgumentNullException>("referencedProject", () =>
            {
                return checker.CanAddProjectReferenceAsync(null!);
            });
        }

        [Fact]
        public void CanAddProjectReferencesAsync_NullAsReferencedProjects_ThrowsArgumentNull()
        {
            var checker = CreateInstance();

            Assert.ThrowsAsync<ArgumentNullException>("referencedProjects", () =>
            {
                return checker.CanAddProjectReferencesAsync(null!);
            });
        }

        [Fact]
        public void CanAddProjectReferencesAsync_EmptyAsReferencedProjects_ThrowsArgument()
        {
            var checker = CreateInstance();

            Assert.ThrowsAsync<ArgumentException>("referencedProjects", () =>
            {
                return checker.CanAddProjectReferencesAsync(ImmutableHashSet<object>.Empty);
            });
        }

        [Fact]
        public void CanBeReferencedAsync_NullAsReferencingProject_ThrowsArgumentNull()
        {
            var checker = CreateInstance();

            Assert.ThrowsAsync<ArgumentNullException>("referencingProject", () =>
            {
                return checker.CanBeReferencedAsync(null!);
            });
        }

        [Fact]
        public async Task CanAddProjectReferenceAsync_ReturnsSupported()
        {
            var project = new object();
            var checker = CreateInstance();

            var result = await checker.CanAddProjectReferenceAsync(project);

            Assert.Equal(SupportedCheckResult.Supported, result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task CanAddProjectReferencesAsync_ReturnsErrorMessageSetToNull(int count)
        {
            var checker = CreateInstance();
            var referencedProjects = CreateSet(count);

            var result = await checker.CanAddProjectReferencesAsync(referencedProjects);

            Assert.Null(result.ErrorMessage);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task CanAddProjectReferencesAsync_ReturnsAsManyIndividualResultsAsProjects(int count)
        {
            var checker = CreateInstance();
            var referencedProjects = CreateSet(count);

            var result = await checker.CanAddProjectReferencesAsync(referencedProjects);

            Assert.Equal(result.IndividualResults.Keys, referencedProjects);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task CanAddProjectReferencesAsync_ReturnsAllResultsSetToSupported(int count)
        {
            var checker = CreateInstance();
            var referencedProjects = CreateSet(count);

            var result = await checker.CanAddProjectReferencesAsync(referencedProjects);

            Assert.All(result.IndividualResults.Values, r => Assert.Equal(SupportedCheckResult.Supported, r));
        }

        [Fact]
        public async Task CanBeReferencedAsync_ReturnsSupported()
        {
            var project = new object();
            var checker = CreateInstance();

            var result = await checker.CanBeReferencedAsync(project);

            Assert.Equal(SupportedCheckResult.Supported, result);
        }

        private static IImmutableSet<object> CreateSet(int count)
        {
            var builder = ImmutableHashSet.CreateBuilder<object>();

            for (int i = 0; i < count; i++)
            {
                builder.Add(new object());
            }

            return builder.ToImmutableHashSet();
        }

        private static AlwaysAllowValidProjectReferenceChecker CreateInstance()
        {
            return new AlwaysAllowValidProjectReferenceChecker();
        }
    }
}
