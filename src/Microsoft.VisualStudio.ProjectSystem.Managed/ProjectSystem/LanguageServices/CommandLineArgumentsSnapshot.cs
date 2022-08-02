// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    /// Immutable snapshot containing the list of compiler command line arguments advertised by the project's <c>CompileDesignTime</c> target.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Produced by <see cref="CommandLineArgumentsProvider"/>.
    /// <para>
    /// </para>
    ///     This snapshot exists because CPS's dataflow/rules handling does not preserve the order of items.
    ///     The order of command line arguments is important, so we create a dedicated snapshot and dataflow
    ///     source for them.
    /// </para>
    /// </remarks>
    internal sealed class CommandLineArgumentsSnapshot
    {
        /// <summary>
        /// Gets the list of compiler arguments returned by the project's <c>CompileDesignTime</c> target.
        /// </summary>
        public ImmutableArray<string> Arguments { get; }

        /// <summary>
        /// Gets whether the set of arguments has changed since the last snapshot was produced.
        /// Allows skipping redundant downstream work when no changes occur.
        /// </summary>
        public bool IsChanged { get; }

        public CommandLineArgumentsSnapshot(ImmutableArray<string> arguments, bool isChanged)
        {
            Arguments = arguments;
            IsChanged = isChanged;
        }
    }
}
