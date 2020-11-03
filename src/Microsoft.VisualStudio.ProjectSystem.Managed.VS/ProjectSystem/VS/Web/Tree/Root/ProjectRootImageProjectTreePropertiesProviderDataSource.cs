// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using PropertiesProviderProjectValue = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<Microsoft.VisualStudio.ProjectSystem.IProjectTreePropertiesProvider>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web.Tree
{
    [Export(typeof(IProjectTreePropertiesProviderDataSource))]
    [AppliesTo(ProjectCapability.DotNet)] 
    internal class ProjectRootImageProjectTreePropertiesProviderDataSource : ChainedProjectValueDataSourceBase<IProjectTreePropertiesProvider>, IProjectTreePropertiesProviderDataSource
    {
        private static readonly IReadOnlyList<ProjectRootImage> s_rootImages = CreateProjectRootImages();
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public ProjectRootImageProjectTreePropertiesProviderDataSource(
            UnconfiguredProject project)
            : base(project, synchronousDisposal: true, registerDataSource: true)
        {
            _project = project;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<PropertiesProviderProjectValue> targetBlock)
        {
            JoinUpstreamDataSources(_project.Capabilities);

            DisposableValue<ISourceBlock<PropertiesProviderProjectValue>> block =
                _project.Capabilities.SourceBlock.TransformMany(CreateProvider);

            block.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return block;
        }

        private static IEnumerable<PropertiesProviderProjectValue> CreateProvider(IProjectVersionedValue<IProjectCapabilitiesSnapshot> snapshot)
        {
            ProjectRootImage? image = s_rootImages.Where(i => AppliesTo(i.AppliesTo, snapshot.Value))
                                                  .FirstOrDefault();

            if (image == null)
                return Enumerable.Empty<PropertiesProviderProjectValue>();

            return new[] { new ProjectVersionedValue<IProjectTreePropertiesProvider>(image.Provider, snapshot.DataSourceVersions) };
        }

        private static bool AppliesTo(string appliesTo, IProjectCapabilitiesSnapshot snapshot)
        {
            return CommonProjectSystemTools.IsCapabilityMatch(
                appliesTo,
                (symbol, tester) => ((IProjectCapabilitiesSnapshot)tester).IsProjectCapabilityPresent(symbol),
                snapshot);
        }

        private static IReadOnlyList<ProjectRootImage> CreateProjectRootImages()
        {
            return new List<ProjectRootImage>()
            {
                // TODO: Capabilities & VB/FSharp
                ProjectRootImage.Create("AspNet",                                          KnownMonikers.CSWebApplication),
                ProjectRootImage.Create(ProjectCapability.WPF + " & " + "WindowsExe",              KnownMonikers.CSWPFApplication),
                ProjectRootImage.Create(ProjectCapability.WPF + " & " + "Library",                 KnownMonikers.CSWPFLibrary),
                ProjectRootImage.Create(ProjectCapability.WindowsForms + " & " + "WindowsExe",     KnownMonikers.CSApplication),
                ProjectRootImage.Create(ProjectCapability.WindowsForms + " & " + "Library",        KnownMonikers.CSWindowsLibary),
                ProjectRootImage.Create("ConsoleExe",                                         KnownMonikers.CSConsole),
                ProjectRootImage.Create("Library",                                         KnownMonikers.CSClassLibrary),
                ProjectRootImage.Create("",                                                KnownMonikers.CSProjectNode),
            };
        }

        private record ProjectRootImage
        (
            string AppliesTo,
            IProjectTreePropertiesProvider Provider
        )
        {
            public static ProjectRootImage Create(string appliesTo, ImageMoniker moniker)
            {
                return new ProjectRootImage(appliesTo, new ProjectRootImageProjectTreePropertiesProvider(moniker.ToProjectSystemType()));
            }
        }
    }
}
