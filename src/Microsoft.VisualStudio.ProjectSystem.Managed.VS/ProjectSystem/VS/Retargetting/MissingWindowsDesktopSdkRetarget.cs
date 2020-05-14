// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [Export(typeof(IProjectRetargetCheckProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class MissingWindowsDesktopSdkRetarget : IProjectRetargetCheckProvider
    {
        /// <summary>
        /// Wether the retargeting option should always be available, to provide a nice UI to the user, or only when a desktop target is not present, to fix up problems
        /// </summary>
        private const bool AlwaysProvideRetargetingOption = false;

        private readonly ConfiguredProject _project;
        private readonly IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> _retargettingService;
        private readonly IProjectAccessor _projectAccessor;

        private DesktopPlatformTargetDescription? _targetDescription;

        [ImportingConstructor]
        internal MissingWindowsDesktopSdkRetarget(ConfiguredProject project,
                                                  IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> retargettingService,
                                                  IProjectAccessor projectAccessor)
        {
            _project = project;
            _retargettingService = retargettingService;
            _projectAccessor = projectAccessor;
        }

        public Task<IProjectTargetChange?> CheckAsync(IImmutableDictionary<string, IProjectRuleSnapshot> projectState)
        {
            var useWpf = projectState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, "UseWPF", null);
            var useWindowsForms = projectState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, "UseWindowsForms", null);

            if (useWpf == null && useWindowsForms == null)
            {
                return Task.FromResult((IProjectTargetChange?)new ProjectTargetChange(new DesktopPlatformTargetDescription().TargetId));
            }

            return Task.FromResult((IProjectTargetChange?)null);
        }

        //public async Task<IProjectTargetChange?> CheckForRetargetAsync(RetargetCheckOptions options)
        //{
        //    await Initialize();

        //    Assumes.NotNull(_targetDescription);

        //    if ((options & RetargetCheckOptions.ProjectLoad) == RetargetCheckOptions.ProjectLoad)
        //    {
        //        return await _projectAccessor.OpenProjectXmlForReadAsync(_project, root =>
        //        {
        //            if (root.Sdk == "Microsoft.NET.Sdk.WindowsDesktop" &&
        //                (AlwaysProvideRetargetingOption || !root.Properties.Any(p => p.Name == "UseWindowsForms" || p.Name == "UseWPF")))
        //            {
        //                return new ProjectTargetChange(_targetDescription.TargetId);
        //            }
        //            return null;
        //        });
        //    }
        //    else
        //    {
        //        switch (options)
        //        {
        //            case RetargetCheckOptions.None:
        //                break;
        //            case RetargetCheckOptions.NoPrompt:
        //                break;
        //            case RetargetCheckOptions.RequiredOnly:
        //                break;
        //            case RetargetCheckOptions.FirstSolutionLoad:
        //                break;
        //            case RetargetCheckOptions.ProjectLoad:
        //                break;
        //            case RetargetCheckOptions.ProjectRetarget:
        //                break;
        //            case RetargetCheckOptions.ProjectReload:
        //                break;
        //            case RetargetCheckOptions.SolutionRetarget:
        //                break;
        //            default:
        //                break;
        //        }
        //    }

        //    return null;
        //}

        private async Task Initialize()
        {
            if (_targetDescription == null)
            {
                _targetDescription = new DesktopPlatformTargetDescription();
                IVsTrackProjectRetargeting2 trackProjectRetageting = await _retargettingService.GetValueAsync();
                trackProjectRetageting.RegisterProjectTarget(_targetDescription);
            }
        }

        //public Task<IImmutableList<string>> GetAffectedFilesAsync(IProjectTargetChange projectTargetChange)
        //{
        //    return Task.FromResult((IImmutableList<string>)ImmutableList<string>.Empty.Add(_project.FullPath));
        //}

        //public async Task RetargetAsync(TextWriter outputLogger, RetargetOptions options, IProjectTargetChange projectTargetChange, string backupLocation)
        //{
        //    if (options == RetargetOptions.Backup)
        //    {
        //        outputLogger.WriteLine("Bkacing up to " + backupLocation);
        //        File.Copy(_project.FullPath, Path.Combine(backupLocation, Path.GetFileName(_project.FullPath)));
        //    }

        //    IVsTrackProjectRetargeting2 trackProjectRetageting = await _retargettingService.GetValueAsync();
        //    if (ErrorHandler.Succeeded(trackProjectRetageting.GetProjectTarget(projectTargetChange.NewTargetId, out IVsProjectTargetDescription description)) && description is DesktopPlatformTargetDescription desktopPlatformTarget)
        //    {
        //         await _projectAccessor.OpenProjectXmlForWriteAsync(_project, root =>
        //         {
        //             Microsoft.Build.Construction.ProjectPropertyElement prop = BuildUtilities.GetOrAddProperty(root, "Use" + desktopPlatformTarget.NewTargetPlatformName);
        //             prop.Value = "true";

        //             root.Save();
        //         });
        //    }
        //}



        internal class ProjectTargetChange : IProjectTargetChange
        {
            private readonly Guid _targetId;

            public ProjectTargetChange(Guid targetId)
            {
                _targetId = targetId;
            }

            public Guid NewTargetId => _targetId;

            public Guid CurrentTargetId => Guid.Empty;

            public bool ReloadProjectOnSuccess => true;

            public bool UnloadOnFailure => true;

            public bool UnloadOnCancel => true;
        }
    }
}
