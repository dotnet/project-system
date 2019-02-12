// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Versioning;

using NuGet.VisualStudio;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    [Export(typeof(ITargetFrameworkProvider))]
    internal sealed class TargetFrameworkProvider : ITargetFrameworkProvider
    {
        private readonly IVsFrameworkCompatibility _nuGetComparer;
        private readonly IVsFrameworkParser _nuGetFrameworkParser;

        /// <summary>
        /// Lookup for known <see cref="ITargetFramework"/> objects, keyed by both
        /// <see cref="ITargetFramework.ShortName"/> and <see cref="ITargetFramework.FullName"/>.
        /// </summary>
        private ImmutableDictionary<string, ITargetFramework> _targetFrameworkByName = ImmutableDictionary.Create<string, ITargetFramework>(StringComparer.Ordinal);

        [ImportingConstructor]
        public TargetFrameworkProvider(
            IVsFrameworkCompatibility nugetComparer,
            IVsFrameworkParser nugetFrameworkParser)
        {
            _nuGetComparer = nugetComparer;
            _nuGetFrameworkParser = nugetFrameworkParser;
        }

        public ITargetFramework GetTargetFramework(string shortOrFullName)
        {
            if (string.IsNullOrEmpty(shortOrFullName))
            {
                return null;
            }

            // Fast path for an exact name match
            if (_targetFrameworkByName.TryGetValue(shortOrFullName, out ITargetFramework existing))
            {
                return existing;
            }

            try
            {
                // Try to parse a short or full framework name
                FrameworkName frameworkName = _nuGetFrameworkParser.ParseFrameworkName(shortOrFullName);

                if (frameworkName == null)
                {
                    return null;
                }

                if (_targetFrameworkByName.TryGetValue(frameworkName.FullName, out ITargetFramework exitingByFullName))
                {
                    // The full name was known, so cache by the provided (unknown) name too for next time
                    ImmutableInterlocked.TryAdd(ref _targetFrameworkByName, shortOrFullName, exitingByFullName);

                    return exitingByFullName;
                }

                string shortName = _nuGetFrameworkParser.GetShortFrameworkName(frameworkName);

                if (shortName != null && _targetFrameworkByName.TryGetValue(shortName, out ITargetFramework exitingByShortName))
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

        public ITargetFramework GetNearestFramework(ITargetFramework targetFramework,
                                                    IEnumerable<ITargetFramework> otherFrameworks)
        {
            if (targetFramework?.FrameworkName == null || otherFrameworks == null)
            {
                return null;
            }

            var others = otherFrameworks.Where(other => other.FrameworkName != null).ToList();

            if (others.Count == 0)
            {
                return null;
            }

            FrameworkName nearestFrameworkName = _nuGetComparer.GetNearest(
                targetFramework.FrameworkName, others.Select(x => x.FrameworkName));

            if (nearestFrameworkName == null)
            {
                return null;
            }

            return others.FirstOrDefault((x, nearest) => nearest.Equals(x.FrameworkName), nearestFrameworkName);
        }
    }
}
