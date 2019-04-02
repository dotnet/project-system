// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.VisualStudio;
using NuGet.Frameworks;

namespace NuGetCopied.VisualStudio
{
    [Export(typeof(IVsFrameworkCompatibility))]
    [Export(typeof(IVsFrameworkCompatibility2))]
    internal class VsFrameworkCompatibility : IVsFrameworkCompatibility2
    {
        public IEnumerable<FrameworkName> GetNetStandardFrameworks()
        {
            return DefaultFrameworkNameProvider
                .Instance
                .GetNetStandardVersions()
                .Select(framework => new FrameworkName(framework.DotNetFrameworkName));
        }

        public IEnumerable<FrameworkName> GetFrameworksSupportingNetStandard(FrameworkName frameworkName)
        {
            if (frameworkName == null)
            {
                throw new ArgumentNullException(nameof(frameworkName));
            }

            var nuGetFramework = NuGetFramework.ParseFrameworkName(frameworkName.ToString(), DefaultFrameworkNameProvider.Instance);

            if (!StringComparer.OrdinalIgnoreCase.Equals(
                nuGetFramework.Framework,
                FrameworkConstants.FrameworkIdentifiers.NetStandard))
            {
                throw new ArgumentException(string.Format(
                    Resources.InvalidNetStandardFramework,
                    frameworkName));
            }

            return CompatibilityListProvider
                .Default
                .GetFrameworksSupporting(nuGetFramework)
                .Select(framework => new FrameworkName(framework.DotNetFrameworkName));
        }

        public FrameworkName GetNearest(FrameworkName targetFramework, IEnumerable<FrameworkName> frameworks)
        {
            return GetNearest(targetFramework, Enumerable.Empty<FrameworkName>(), frameworks);
        }

        public FrameworkName GetNearest(FrameworkName targetFramework, IEnumerable<FrameworkName> fallbackTargetFrameworks, IEnumerable<FrameworkName> frameworks)
        {
            if (targetFramework == null)
            {
                throw new ArgumentNullException(nameof(targetFramework));
            }

            if (fallbackTargetFrameworks == null)
            {
                throw new ArgumentNullException(nameof(fallbackTargetFrameworks));
            }

            if (frameworks == null)
            {
                throw new ArgumentNullException(nameof(frameworks));
            }

            var nuGetTargetFramework = NuGetFramework.ParseFrameworkName(targetFramework.ToString(), DefaultFrameworkNameProvider.Instance);

            var nuGetFallbackTargetFrameworks = fallbackTargetFrameworks
                .Select(framework => NuGetFramework.ParseFrameworkName(framework.ToString(), DefaultFrameworkNameProvider.Instance))
                .ToList();

            if (nuGetFallbackTargetFrameworks.Any())
            {
                nuGetTargetFramework = new FallbackFramework(nuGetTargetFramework, nuGetFallbackTargetFrameworks);
            }

            var nuGetFrameworks = frameworks
                .Select(framework => NuGetFramework.ParseFrameworkName(framework.ToString(), DefaultFrameworkNameProvider.Instance));

            var reducer = new FrameworkReducer();
            var nearest = reducer.GetNearest(nuGetTargetFramework, nuGetFrameworks);

            if (nearest == null)
            {
                return null;
            }

            return new FrameworkName(nearest.DotNetFrameworkName);
        }
    }
}
