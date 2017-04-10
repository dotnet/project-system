// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    [Export(typeof(ITargetFrameworkProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class TargetFrameworkProvider : OnceInitializedOnceDisposed, ITargetFrameworkProvider
    {
        [ImportingConstructor]
        public TargetFrameworkProvider(SVsServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
        
        private SVsServiceProvider ServiceProvider { get; }
        protected IVsFrameworkCompatibility NugetComparer { get; set; }
        protected IVsFrameworkParser NugetFrameworkParser { get; set; }

        private readonly object _targetsLock = new object();
        private List<ITargetFramework> CachedTargetFrameworks = new List<ITargetFramework>();

        protected override void Initialize()
        {
            if (!ShouldInitialize())
            {
                return;
            }

            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var container = ServiceProvider.GetService<IComponentModel, SComponentModel>();
                if (container != null)
                {
                    NugetComparer = container.GetExtensions<IVsFrameworkCompatibility>().FirstOrDefault();
                    NugetFrameworkParser = container.GetExtensions<IVsFrameworkParser>().FirstOrDefault();
                }
            });
        }

        /// <summary>
        /// For unit tests to avoid UI initialization.
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldInitialize()
        {
            return NugetComparer == null || NugetFrameworkParser == null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NugetComparer = null;
                NugetFrameworkParser = null;
            }
        }

        public ITargetFramework GetTargetFramework(string shortOrFullName)
        {
            EnsureInitialized();

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
                        // remember target framework - there can not bee too many of them accross the solution.
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
            EnsureInitialized();

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