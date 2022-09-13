// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// A utility class for zero-allocation matching of <see cref="ProjectTreeFlags"/> associated with hierarchy items created by CPS.
    /// </summary>
    /// <remarks>
    /// Instances of this type should be reused as they construct <see cref="Regex"/> objects internally.
    /// </remarks>
    internal readonly struct FlagsStringMatcher
    {
        private readonly Regex? _regex;

        public FlagsStringMatcher(ProjectTreeFlags flags, RegexOptions options = RegexOptions.Compiled)
        {
            switch (flags.Count)
            {
                case 0:
                    // We are testing against the empty set of flags, which always returns true
                    _regex = null;
                    break;
                case 1:
                    // Find the single flag, using full-word search
                    _regex = new Regex($@"\b({flags.First()})\b", options);
                    break;
                default:
                    // Find all flags, in any order, using full-word search
                    // https://regex101.com/r/LPVGgB/1
                    var pattern = new StringBuilder("^");

                    foreach (string flagName in flags)
                    {
                        pattern.AppendFormat(@"(?=.*\b({0})\b)", flagName);
                    }

                    pattern.Append(".*$");

                    _regex = new Regex(pattern.ToString(), options);
                    break;
            }
        }

        public bool Matches(string flagsString)
        {
            Requires.NotNull(flagsString, nameof(flagsString));

            if (_regex is null)
            {
                // We are testing against the empty set of flags, which always returns true
                return true;
            }

            return _regex.IsMatch(flagsString);
        }
    }
}
