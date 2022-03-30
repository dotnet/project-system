// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.References
{
    /// <summary>
    ///     Provides an implementation of <see cref="IValidProjectReferenceChecker"/> that always allows a reference.
    /// </summary>
    /// <remarks>
    ///     Unlike the old project system, we do zero validation when we get added as a project reference, or when
    ///     we reference another project. Instead, we push off all validation to MSBuild targets that do the validation
    ///     during builds and design-time builds (ResolveProjectReferences). This gives a consistent behavior between
    ///     adding the project reference via the UI and adding the project via the editor - in the end they result in the
    ///     same behavior, inside and outside of Visual Studio.
    /// </remarks>
    [Export(typeof(IValidProjectReferenceChecker))]
    [AppliesTo(ProjectCapability.DotNet)]
    [Order(Order.Default)] // Before the default checker, which delegates onto normal P-2-P rules
    internal class AlwaysAllowValidProjectReferenceChecker : IValidProjectReferenceChecker
    {
        private static readonly Task<SupportedCheckResult> s_supported = Task.FromResult(SupportedCheckResult.Supported);

        [ImportingConstructor]
        public AlwaysAllowValidProjectReferenceChecker()
        {
        }

        public Task<SupportedCheckResult> CanAddProjectReferenceAsync(object referencedProject)
        {
            Requires.NotNull(referencedProject, nameof(referencedProject));

            return s_supported;
        }

        public Task<CanAddProjectReferencesResult> CanAddProjectReferencesAsync(IImmutableSet<object> referencedProjects)
        {
            Requires.NotNullEmptyOrNullElements(referencedProjects, nameof(referencedProjects));

            IImmutableDictionary<object, SupportedCheckResult> results = ImmutableDictionary.Create<object, SupportedCheckResult>();

            foreach (object referencedProject in referencedProjects)
            {
                results = results.Add(referencedProject, SupportedCheckResult.Supported);
            }

            return Task.FromResult(new CanAddProjectReferencesResult(results, null));
        }

        public Task<SupportedCheckResult> CanBeReferencedAsync(object referencingProject)
        {
            Requires.NotNull(referencingProject, nameof(referencingProject));

            return s_supported;
        }
    }
}
