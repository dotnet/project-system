// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices.CSharp
{
    /// <summary>
    ///     An implementation of <see cref="ILanguageFeaturesProvider"/> to provider C# language-specific features.
    /// </summary>
    [Export(typeof(ILanguageFeaturesProvider))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpLanguageFeaturesProvider : ILanguageFeaturesProvider
    {
        private static readonly ImmutableHashSet<UnicodeCategory> s_identifierCharCategories = ImmutableHashSet<UnicodeCategory>.Empty
            .Add(UnicodeCategory.UppercaseLetter)
            .Add(UnicodeCategory.LowercaseLetter)
            .Add(UnicodeCategory.TitlecaseLetter)
            .Add(UnicodeCategory.ModifierLetter)
            .Add(UnicodeCategory.OtherLetter)
            .Add(UnicodeCategory.DecimalDigitNumber)
            .Add(UnicodeCategory.ConnectorPunctuation)
            .Add(UnicodeCategory.EnclosingMark)
            .Add(UnicodeCategory.NonSpacingMark);

        private static readonly ImmutableHashSet<UnicodeCategory> s_firstIdentifierCharCategories = ImmutableHashSet<UnicodeCategory>.Empty
            .Add(UnicodeCategory.UppercaseLetter)
            .Add(UnicodeCategory.LowercaseLetter)
            .Add(UnicodeCategory.TitlecaseLetter)
            .Add(UnicodeCategory.ModifierLetter)
            .Add(UnicodeCategory.OtherLetter)
            .Add(UnicodeCategory.ConnectorPunctuation);

        [ImportingConstructor]
        public CSharpLanguageFeaturesProvider()
        {
        }

        /// <summary>
        ///     Makes a proper language identifier from the specified name.
        /// </summary>
        /// <param name="name">
        ///     A <see cref="string"/> containing the name.
        /// </param>
        /// <returns>
        ///     A proper identifier which meets the C# language specification.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="name"/> is an empty string ("").
        /// </exception>
        public string MakeProperIdentifier(string name)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            string identifier = string.Concat(name.Select(c => IsValidIdentifierChar(c) ? c : '_'));
            if (!IsValidFirstIdentifierChar(identifier[0]))
            {
                identifier = '_' + identifier;
            }

            return identifier;
        }

        /// <summary>
        ///     Makes a proper namespace from the specified name.
        /// </summary>
        /// <param name="name">
        ///     A <see cref="string"/> containing the name.
        /// </param>
        /// <returns>
        ///     A proper namespace which meets the C# language specification.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="name"/> is an empty string ("").
        /// </exception>
        public string MakeProperNamespace(string name)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            IEnumerable<string> namespaceNames = new LazyStringSplit(name, '.').Select(MakeProperIdentifier);

            return string.Join(".", namespaceNames);
        }

        /// <summary>
        ///     Concatenates the specified namespace names.
        /// </summary>
        /// <param name="namespaceNames">
        ///     A <see cref="string"/> array containing the namespace names to be concatenated.
        /// </param>
        /// <returns>
        ///     A concatenated namespace name.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="namespaceNames"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="namespaceNames"/> contains no elements.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="namespaceNames"/> contains an element that is <see langword="null"/>.
        /// </exception>
        public string ConcatNamespaces(params string[] namespaceNames)
        {
            Requires.NotNullEmptyOrNullElements(namespaceNames, nameof(namespaceNames));

            return string.Join(".", namespaceNames.Where(name => name.Length > 0));
        }

        private static bool IsValidIdentifierChar(char c)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            return s_identifierCharCategories.Contains(category);
        }

        private static bool IsValidFirstIdentifierChar(char c)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            return s_firstIdentifierCharCategories.Contains(category);
        }
    }
}
