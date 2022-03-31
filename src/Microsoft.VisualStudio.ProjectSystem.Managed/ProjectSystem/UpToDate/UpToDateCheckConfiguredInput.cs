// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Models all up-to-date check state for a single configuration. 
    /// </summary>
    /// <remarks>
    /// Produced by <see cref="IUpToDateCheckImplicitConfiguredInputDataSource"/>.
    /// </remarks>
    internal sealed class UpToDateCheckConfiguredInput
    {
        /// <summary>
        /// Gets the up-to-date check state associated with each of the implicitly active
        /// configurations.
        /// </summary>
        public ImmutableArray<UpToDateCheckImplicitConfiguredInput> ImplicitInputs { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpToDateCheckConfiguredInput"/> class.
        /// </summary>
        public UpToDateCheckConfiguredInput(ImmutableArray<UpToDateCheckImplicitConfiguredInput> implicitInputs)
        {
            ImplicitInputs = implicitInputs;
        }
    }
}
