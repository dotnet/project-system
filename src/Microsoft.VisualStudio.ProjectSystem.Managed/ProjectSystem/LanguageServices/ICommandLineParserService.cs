// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Parses <see cref="BuildOptions"/> instances from string-based command-line arguments.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface ICommandLineParserService
    {
        /// <summary>
        ///     Parses the specified string-based command-line arguments.
        /// </summary>
        /// <param name="arguments">
        ///     A <see cref="IEnumerable{T}"/> of <see cref="string"/> representing the individual command-line
        ///     arguments.
        /// </param>
        /// <param name="baseDirectory">
        ///     A <see cref="string"/> containing the base directory used for qualifying file locations.
        /// </param>
        /// <returns>
        ///     An <see cref="BuildOptions"/> representing the result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="arguments"/> is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="baseDirectory"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="baseDirectory"/> is an empty string ("").
        /// </exception>
        BuildOptions Parse(IEnumerable<string> arguments, string baseDirectory);
    }
}
