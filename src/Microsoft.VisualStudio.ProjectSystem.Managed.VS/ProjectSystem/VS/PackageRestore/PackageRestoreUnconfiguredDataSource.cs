// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks.Dataflow;

using Microsoft.VisualStudio.ProjectSystem.Utilities;

using NuGet.SolutionRestoreManager;

using RestoreInfo = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<NuGet.SolutionRestoreManager.IVsProjectRestoreInfo2>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    [Export(typeof(IPackageRestoreUnconfiguredDataSource))]
    [AppliesTo(ProjectCapability.PackageReferences)]
    internal partial class PackageRestoreUnconfiguredDataSource : ChainedProjectValueDataSourceBase<IVsProjectRestoreInfo2>, IPackageRestoreUnconfiguredDataSource
    {
        private readonly UnconfiguredProject _project;
        private readonly IActiveConfigurationGroupService _activeConfigurationGroupService;

        [ImportingConstructor]
        public PackageRestoreUnconfiguredDataSource(UnconfiguredProject project, IActiveConfigurationGroupService activeConfigurationGroupService)
            : base(project.Services, synchronousDisposal: true, registerDataSource: false)
        {
            _project = project;
            _activeConfigurationGroupService = activeConfigurationGroupService;
        }

        protected override UnconfiguredProject ContainingProject
        {
            get { return _project; }
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<RestoreInfo> targetBlock)
        {
            // At a high-level, we want to combine all implicitly active configurations (ie the active config of each TFM) restore data
            // (via ProjectRestoreUpdate) and combine it into a single IVsProjectRestoreInfo2 instance and publish that. When a change is 
            // made to a configuration, such as adding a PackageReference, we should react to it and push a new version of our output. If the 
            // active configuration changes, we should react to it, and publish data from the new set of implicitly active configurations.
            var disposables = new DisposableBag();

            var packageRestoreConfiguredSource = new UnwrapCollectionChainedProjectValueDataSource<IReadOnlyCollection<ConfiguredProject>, ProjectRestoreUpdate>(
                _project.Services, 
                projects => projects.Select(project => GetProjectRestoreDataSource(project)),
                includeSourceVersions: true);

            disposables.AddDisposable(packageRestoreConfiguredSource);

            IProjectValueDataSource<IConfigurationGroup<ConfiguredProject>> activeConfiguredProjectsSource = _activeConfigurationGroupService.ActiveConfiguredProjectGroupSource;
            disposables.AddDisposable(activeConfiguredProjectsSource.SourceBlock.LinkTo(packageRestoreConfiguredSource, DataflowOption.PropagateCompletion));

            // Transform all restore data -> combined restore data
            DisposableValue<ISourceBlock<RestoreInfo>> mergeBlock = packageRestoreConfiguredSource.SourceBlock
                                                                                                  .TransformWithNoDelta(update => update.Derive(MergeRestoreData));
            disposables.AddDisposable(mergeBlock);

            // Set the link up so that we publish changes to target block
            mergeBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete 
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(packageRestoreConfiguredSource, activeConfiguredProjectsSource);

            return disposables;
        }

        private IVsProjectRestoreInfo2 MergeRestoreData(IReadOnlyCollection<ProjectRestoreUpdate> updates)
        {
            // We have no active configuration
            if (updates.Count == 0)
                return null;

            // We need to combine the snapshots from each implicitly active configuration (ie per TFM), 
            // resolving any conflicts, which we'll report to the user. 

            string msbuildProjectExtensionsPath = ResolveMSBuildProjectExtensionsPathConflicts(updates);
            string originalTargetFrameworks = ResolveOriginalTargetFrameworksConflicts(updates);
            IVsReferenceItems toolReferences = ResolveToolReferenceConflicts(updates);
            IVsTargetFrameworks2 targetFrameworks = GetAllTargetFrameworks(updates);

            return new ProjectRestoreInfo(
                msbuildProjectExtensionsPath,
                originalTargetFrameworks,
                targetFrameworks,
                toolReferences);
        }

        private string ResolveMSBuildProjectExtensionsPathConflicts(IEnumerable<ProjectRestoreUpdate> updates)
        {
            // All configurations need to agree on where the project-wide asset file is located.
            return ResolvePropertyConflicts(updates, u => u.BaseIntermediatePath, NuGetRestore.MSBuildProjectExtensionsPathProperty);
        }

        private string ResolveOriginalTargetFrameworksConflicts(IEnumerable<ProjectRestoreUpdate> updates)
        {
            // All configurations need to agree on what the overall "user-written" frameworks for the 
            // project so that conditions in the project-wide 'nuget.g.props' and 'nuget.g.targets' 
            // are written and evaluated correctly.
            return ResolvePropertyConflicts(updates, u => u.OriginalTargetFrameworks, NuGetRestore.TargetFrameworksProperty);
        }

        private string ResolvePropertyConflicts(IEnumerable<ProjectRestoreUpdate> updates, Func<IVsProjectRestoreInfo2, string> propertyGetter, string propertyName)
        {
            // Always use the first TFM listed in project to provide consistent behavior
            ProjectRestoreUpdate update = updates.First();
            string propertyValue = propertyGetter(update.RestoreInfo);

            // Every config should had same value
            bool hasConflicts = updates.Select(u => propertyGetter(u.RestoreInfo))
                                       .Distinct(StringComparers.PropertyNames)
                                       .Count() > 1;

            if (hasConflicts)
            {
                ReportDataSourceUserFault(
                    new Exception(string.Format(
                        CultureInfo.CurrentCulture,
                        VSResources.Restore_PropertyWithInconsistentValues,
                        propertyName,
                        propertyValue,
                        update.ProjectConfiguration)),
                    ProjectFaultSeverity.LimitedFunctionality,
                    ContainingProject);
            }

            return propertyValue;
        }

        private IVsReferenceItems ResolveToolReferenceConflicts(IEnumerable<ProjectRestoreUpdate> updates)
        {
            var references = new Dictionary<string, IVsReferenceItem>(StringComparers.ItemNames);

            foreach (ProjectRestoreUpdate update in updates)
            {
                foreach (IVsReferenceItem reference in update.RestoreInfo.ToolReferences)
                {
                    if (ValidateToolReference(references, reference))
                    {
                        references.Add(reference.Name, reference);
                    }
                }
            }

            return new ReferenceItems(references.Values);
        }
        private IVsTargetFrameworks2 GetAllTargetFrameworks(IEnumerable<ProjectRestoreUpdate> updates)
        {
            var frameworks = new List<IVsTargetFrameworkInfo2>();

            foreach (ProjectRestoreUpdate update in updates)
            {
                Assumes.True(update.RestoreInfo.TargetFrameworks.Count == 1);

                IVsTargetFrameworkInfo2 framework = update.RestoreInfo.TargetFrameworks.Item(0);

                if (ValidateTargetFramework(update.ProjectConfiguration, framework))
                {
                    frameworks.Add(framework);
                }
            }

            return new TargetFrameworks(frameworks);
        }

        private bool ValidateToolReference(Dictionary<string, IVsReferenceItem> existingReferences, IVsReferenceItem reference)
        {
            if (existingReferences.TryGetValue(reference.Name, out IVsReferenceItem existingReference))
            {
                // CLI tool references are project-wide, so if they have conflicts in names, 
                // they must have the same metadata, which avoids from having to condition 
                // them so that they only appear in one TFM.
                if (!RestoreComparer.ReferenceItems.Equals(existingReference, reference))
                {
                    ReportDataSourceUserFault(
                        new Exception(string.Format(CultureInfo.CurrentCulture, VSResources.Restore_DuplicateToolReferenceItems, existingReference.Name)),
                        ProjectFaultSeverity.LimitedFunctionality,
                        ContainingProject);
                }

                return false;
            }

            return true;
        }

        private bool ValidateTargetFramework(ProjectConfiguration projectConfiguration, IVsTargetFrameworkInfo2 framework)
        {
            if (framework.TargetFrameworkMoniker.Length == 0)
            {
                ReportDataSourceUserFault(
                    new Exception(string.Format(CultureInfo.CurrentCulture, VSResources.Restore_EmptyTargetFrameworkMoniker, projectConfiguration.Name)),
                    ProjectFaultSeverity.LimitedFunctionality,
                    ContainingProject);

                return false;
            }

            return true;
        }

        private IProjectValueDataSource<ProjectRestoreUpdate> GetProjectRestoreDataSource(ConfiguredProject project)
        {
            // Get the individual configuration's view of the restore data
            IPackageRestoreConfiguredDataSource dataSource = project.Services.ExportProvider.GetExportedValue<IPackageRestoreConfiguredDataSource>();

            // Wrap it in a data source that will drop project version and identity versions so as they will never agree
            // on these versions as they are unique to each configuration. They'll be consistent by all other versions.
            return new DropConfiguredProjectVersionDataSource<ProjectRestoreUpdate>(_project.Services, dataSource);
        }
    }
}
