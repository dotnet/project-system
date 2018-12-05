using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    /// <summary>
    /// This class handles being notified whenever a source file is edited, in our case by a Single File Generator
    /// </summary>
    [Export(typeof(IProjectChangeHintReceiver))]
    [ProjectChangeHintKind(ProjectChangeFileSystemEntityHint.EditedFileAsString)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class TempPEFileEditHintReceiver : IProjectChangeHintReceiver
    {
        private readonly VSBuildManager _buildManager;
        private readonly Lazy<ITempPEBuildManager> _tempPEBuildManager;

        [ImportingConstructor]
        public TempPEFileEditHintReceiver(
            [Import(typeof(BuildManager))]VSBuildManager buildManager,
            Lazy<ITempPEBuildManager> tempPEBuildManager)
        {
            _buildManager = buildManager;
            _tempPEBuildManager = tempPEBuildManager;
        }

        public async Task HintedAsync(IImmutableDictionary<Guid, IImmutableSet<IProjectChangeHint>> hints)
        {
            // We don't want to be the one to realise the Lazy<ITempPEManager> so ignore changes in projects that don't need TempPE
            if (!_buildManager.ProjectNeedsTempPE) return;

            IImmutableSet<IProjectChangeHint> files = hints[ProjectChangeFileSystemEntityHint.EditedFile];
            foreach (IProjectChangeFileHint hint in files)
            {
                foreach (string fileName in hint.Files)
                {
                    // The magic of MEF means that the instance we get from .Value is the same as the build managers instance, so it has all of the info it needs
                    await _tempPEBuildManager.Value.TryFireTempPEDirtyAsync(hint.UnconfiguredProject.MakeRelative(fileName));
                }
            }
        }

        public Task HintingAsync(IProjectChangeHint hint)
        {
            return Task.CompletedTask;
        }
    }
}
