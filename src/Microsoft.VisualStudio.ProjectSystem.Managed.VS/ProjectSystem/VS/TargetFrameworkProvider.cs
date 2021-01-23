// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Runtime.Versioning;
using NuGet.VisualStudio;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(ITargetFrameworkProvider))]
    internal sealed class TargetFrameworkProvider : ITargetFrameworkProvider
    {
        private readonly IVsFrameworkParser _nuGetFrameworkParser;

        /// <summary>
        /// Lookup for known <see cref="TargetFramework"/> objects, keyed by both
        /// <see cref="TargetFramework.ShortName"/> and <see cref="TargetFramework.FullName"/>.
        /// </summary>
        private ImmutableDictionary<string, TargetFramework> _targetFrameworkByName = ImmutableDictionary.Create<string, TargetFramework>(StringComparer.Ordinal);

        [ImportingConstructor]
        public TargetFrameworkProvider(IVsFrameworkParser nugetFrameworkParser)
        {
            _nuGetFrameworkParser = nugetFrameworkParser;
        }

        public TargetFramework? GetTargetFramework(string? shortOrFullName)
        {
            if (Strings.IsNullOrEmpty(shortOrFullName))
            {
                return null;
            }

            // Fast path for an exact name match
            if (_targetFrameworkByName.TryGetValue(shortOrFullName, out TargetFramework? existing))
            {
                return existing;
            }

            try
            {
                // Try to parse a short or full framework name
                FrameworkName? frameworkName = _nuGetFrameworkParser.ParseFrameworkName(shortOrFullName);

                if (frameworkName == null)
                {
                    return null;
                }

                if (_targetFrameworkByName.TryGetValue(frameworkName.FullName, out TargetFramework? exitingByFullName))
                {
                    // The full name was known, so cache by the provided (unknown) name too for next time
                    ImmutableInterlocked.TryAdd(ref _targetFrameworkByName, shortOrFullName, exitingByFullName);

                    return exitingByFullName;
                }

                string? shortName = _nuGetFrameworkParser.GetShortFrameworkName(frameworkName);

                if (shortName != null && _targetFrameworkByName.TryGetValue(shortName, out TargetFramework? exitingByShortName))
                {
                    // The short name was known, so cache by the provided (unknown) name too for next time
                    ImmutableInterlocked.TryAdd(ref _targetFrameworkByName, shortOrFullName, exitingByShortName);

                    return exitingByShortName;
                }

                // This is a completely new target framework. Create, cache and return it.
                var targetFramework = new TargetFramework(frameworkName, shortName);

                ImmutableInterlocked.TryAdd(ref _targetFrameworkByName, shortOrFullName, targetFramework);

                return targetFramework;
            }
            catch
            {
                // Note: catching all exceptions and return a generic TargetFramework for given shortOrFullName
                return new TargetFramework(shortOrFullName);
            }
        }
    }
}
