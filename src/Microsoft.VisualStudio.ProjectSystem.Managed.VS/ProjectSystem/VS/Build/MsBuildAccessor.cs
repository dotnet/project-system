using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    [Export(typeof(IMsBuildAccessor))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class MsBuildAccessor : IMsBuildAccessor
    {
        private readonly IProjectLockService _projectLockService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public MsBuildAccessor(IProjectLockService projectLockService, UnconfiguredProject unconfiguredProject, IFileSystem fileSystem)
        {
            _projectLockService = projectLockService;
            _unconfiguredProject = unconfiguredProject;
            _fileSystem = fileSystem;
        }

        public async Task<string> GetProjectXmlAsync()
        {
            using (var access = await _projectLockService.ReadLockAsync())
            {
                var stringWriter = new StringWriter();
                var projectXml = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                projectXml.Save(stringWriter);
                return stringWriter.ToString();
            }
        }

        public async Task SubscribeProjectXmlChangedEventAsync(UnconfiguredProject unconfiguredProject, EventHandler<ProjectXmlChangedEventArgs> handler)
        {
            var configuredProject = await unconfiguredProject.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
            using (var access = await _projectLockService.WriteLockAsync())
            {
                var xmlProject = await access.GetProjectAsync(configuredProject).ConfigureAwait(true);

                xmlProject.ProjectCollection.ProjectXmlChanged += handler;
            }
        }

        public async Task UnsubscribeProjectXmlChangedEventAsync(UnconfiguredProject unconfiguredProject, EventHandler<ProjectXmlChangedEventArgs> handler)
        {
            var configuredProject = await unconfiguredProject.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
            using (var access = await _projectLockService.WriteLockAsync())
            {
                var xmlProject = await access.GetProjectAsync(configuredProject).ConfigureAwait(true);

                xmlProject.ProjectCollection.ProjectXmlChanged -= handler;
            }
        }

        public async Task RunLockedAsync(bool writeLock, Func<Task> task)
        {
            if (writeLock)
            {
                using (await _projectLockService.WriteLockAsync())
                {
                    await task().ConfigureAwait(true);
                }
            }
            else
            {
                using (await _projectLockService.ReadLockAsync())
                {
                    await task().ConfigureAwait(true);
                }
            }
        }

        public async Task SaveProjectXmlAsync(string toSave)
        {
            var encoding = await _unconfiguredProject.GetFileEncodingAsync().ConfigureAwait(false);
            using (var access = await _projectLockService.WriteLockAsync())
            {
                await access.CheckoutAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                _fileSystem.WriteAllText(_unconfiguredProject.FullPath, toSave, encoding);
            }
        }
    }
}
