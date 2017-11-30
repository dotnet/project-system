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
        private readonly IVsFrameworkCompatibility _nuGetComparer;
        private readonly IVsFrameworkParser _nuGetFrameworkParser;
        private readonly object _targetsLock = new object();
        private readonly List<ITargetFramework> _cachedTargetFrameworks = new List<ITargetFramework>();

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

            ITargetFramework targetFramework = null;
            try
            {
                lock (_targetsLock)
                {
                    // use linear search here, since there not many target frameworks and it would most efficient.
                    targetFramework = _cachedTargetFrameworks.FirstOrDefault(x => x.Equals(shortOrFullName));
                    if (targetFramework != null)
                    {
                        return targetFramework;
                    }

                    var frameworkName = _nuGetFrameworkParser.ParseFrameworkName(shortOrFullName);
                    if (frameworkName != null)
                    {
                        var shortName = _nuGetFrameworkParser.GetShortFrameworkName(frameworkName);
                        targetFramework = new TargetFramework(frameworkName, shortName);
                        // remember target framework - there can not bee too many of them across the solution.
                        _cachedTargetFrameworks.Add(targetFramework);
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

            var nearestFrameworkName = _nuGetComparer.GetNearest(
                targetFramework.FrameworkName, otherFrameworks.Select(x => x.FrameworkName));
            if (nearestFrameworkName == null)
            {
                return null;
            }

            return otherFrameworks.FirstOrDefault(x => nearestFrameworkName.Equals(x.FrameworkName));
        }
    }
}