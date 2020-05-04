// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [Export(typeof(IProjectRetargetHandler))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class MissingWindowsDesktopSdkRetarget : IProjectRetargetHandler
    {
        /// <summary>
        /// Wether the retargeting option should always be available, to provide a nice UI to the user, or only when a desktop target is not present, to fix up problems
        /// </summary>
        private const bool AlwaysProvideRetargetingOption = true;

        private readonly UnconfiguredProject _project;
        private readonly IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> _retargettingService;
        private readonly IProjectAccessor _projectAccessor;
        private DesktopPlatformTargetDescription? _targetDescription;

        [ImportingConstructor]
        internal MissingWindowsDesktopSdkRetarget(UnconfiguredProject project, IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> retargettingService, IProjectAccessor projectAccessor)
        {
            _project = project;
            _retargettingService = retargettingService;
            _projectAccessor = projectAccessor;
        }

        public async Task<IProjectTargetChange?> CheckForRetargetAsync(RetargetCheckOptions options)
        {
            await Initialize();

            Assumes.NotNull(_targetDescription);

            if (options == RetargetCheckOptions.SolutionRetarget)
            {
                return await _projectAccessor.OpenProjectXmlForReadAsync(_project, root =>
                {
                    if (root.Sdk == "Microsoft.NET.Sdk.WindowsDesktop" &&
                        (AlwaysProvideRetargetingOption || !root.Properties.Any(p => p.Name == "UseWindowsForms" || p.Name == "UseWPF")))
                    {
                        return new ProjectTargetChange(_targetDescription.TargetId);
                    }
                    return null;
                });
            }
            else
            {

                switch (options)
                {
                    case RetargetCheckOptions.None:
                        break;
                    case RetargetCheckOptions.NoPrompt:
                        break;
                    case RetargetCheckOptions.RequiredOnly:
                        break;
                    case RetargetCheckOptions.FirstSolutionLoad:
                        break;
                    case RetargetCheckOptions.ProjectLoad:
                        break;
                    case RetargetCheckOptions.ProjectRetarget:
                        break;
                    case RetargetCheckOptions.ProjectReload:
                        break;
                    default:
                        break;
                }
            }

            return null;
        }

        private async Task Initialize()
        {
            if (_targetDescription == null)
            {
                _targetDescription = new DesktopPlatformTargetDescription();
                IVsTrackProjectRetargeting2 trackProjectRetageting = await _retargettingService.GetValueAsync();
                trackProjectRetageting.RegisterProjectTarget(_targetDescription);
            }
        }

        public Task<IImmutableList<string>> GetAffectedFilesAsync(IProjectTargetChange projectTargetChange)
        {
            return Task.FromResult((IImmutableList<string>)ImmutableList<string>.Empty.Add(_project.FullPath));
        }

        public async Task RetargetAsync(TextWriter outputLogger, RetargetOptions options, IProjectTargetChange projectTargetChange, string backupLocation)
        {
            outputLogger.WriteLine("Hello!");
            if (options == RetargetOptions.Backup)
            {
                outputLogger.WriteLine("Bkacing up to " + backupLocation);
                File.Copy(_project.FullPath, Path.Combine(backupLocation, Path.GetFileName(_project.FullPath)));
            }

            IVsTrackProjectRetargeting2 trackProjectRetageting = await _retargettingService.GetValueAsync();
            if (ErrorHandler.Succeeded(trackProjectRetageting.GetProjectTarget(projectTargetChange.NewTargetId, out IVsProjectTargetDescription description)) && description is DesktopPlatformTargetDescription desktopPlatformTarget)
            {
                 await _projectAccessor.OpenProjectXmlForWriteAsync(_project, root =>
                 {
                     Microsoft.Build.Construction.ProjectPropertyElement prop = BuildUtilities.GetOrAddProperty(root, "Use" + desktopPlatformTarget.NewTargetPlatformName);
                     prop.Value = "true";

                     root.Save();
                 });
            }
        }

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
