using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

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
        private readonly ITempPEBuildManager _tempPEBuildManager;

        [ImportingConstructor]
        public TempPEFileEditHintReceiver(ITempPEBuildManager tempPEBuildManager)
        {
            _tempPEBuildManager = tempPEBuildManager;
        }

        public async Task HintedAsync(IImmutableDictionary<Guid, IImmutableSet<IProjectChangeHint>> hints)
        {
            IImmutableSet<IProjectChangeHint> files = hints[ProjectChangeFileSystemEntityHint.EditedFile];
            foreach (IProjectChangeFileHint hint in files)
            {
                foreach (string fileName in hint.Files)
                {
                    await _tempPEBuildManager.NotifySourceFileDirtyAsync(hint.UnconfiguredProject.MakeRelative(fileName));
                }
            }
        }

        public Task HintingAsync(IProjectChangeHint hint)
        {
            return Task.CompletedTask;
        }
    }
}
