// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using NuGet.VisualStudio;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    [Export(typeof(ITargetFrameworkProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class TargetFrameworkProvider : ITargetFrameworkProvider
    {
        [ImportingConstructor]
        public TargetFrameworkProvider(
            IVsFrameworkCompatibility nugetComparer,
            IVsFrameworkParser nugetFrameworkParser)
        {
            NugetComparer = nugetComparer;
            NugetFrameworkParser = nugetFrameworkParser;
        }

        private IVsFrameworkCompatibility NugetComparer { get; }

        private IVsFrameworkParser NugetFrameworkParser { get; }

        private readonly object _targetsLock = new object();
        private List<ITargetFramework> CachedTargetFrameworks = new List<ITargetFramework>();

        public ITargetFramework GetTargetFramework(string shortOrFullName)
        {
            if (string.IsNullOrEmpty(shortOrFullName))
            {
                return null;
            }

            ITargetFramework targetFramework = null;
            try
            {
                lock (_targetsLock)
                {
                    // use linear search here, since there not many target frameworks and it would most efficient.
                    targetFramework = CachedTargetFrameworks.FirstOrDefault(x => x.Equals(shortOrFullName));
                    if (targetFramework != null)
                    {
                        return targetFramework;
                    }

                    var frameworkName = NugetFrameworkParser.ParseFrameworkName(shortOrFullName);
                    if (frameworkName != null)
                    {
                        var shortName = NugetFrameworkParser.GetShortFrameworkName(frameworkName);
                        targetFramework = new TargetFramework(frameworkName, shortName);
                        // remember target framework - there can not bee too many of them across the solution.
                        CachedTargetFrameworks.Add(targetFramework);
                    }
                }
            }
            catch
            {
                // Note: catching all exceptions and return a generic TargetFramework for given shortOrFullName
                targetFramework = new TargetFramework(shortOrFullName);
            }

            return targetFramework;
        }

        public ITargetFramework GetNearestFramework(ITargetFramework targetFramework,
                                                    IEnumerable<ITargetFramework> otherFrameworks)
        {
            if (targetFramework == null || otherFrameworks == null || !otherFrameworks.Any())
            {
                return null;
            }

            var nearestFrameworkName = NugetComparer.GetNearest(
                targetFramework.FrameworkName, otherFrameworks.Select(x => x.FrameworkName));
            if (nearestFrameworkName == null)
            {
                return null;
            }

            return otherFrameworks.FirstOrDefault(x => nearestFrameworkName.Equals(x.FrameworkName));
        }
    }
}