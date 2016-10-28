using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    /// <summary>
    /// Utility class for allowing for testing of code that needs to access the msbuild lock, and also be testable.
    /// </summary>
    internal interface IMsBuildAccessor
    {
        /// <summary>
        /// Gets the XML for a given unconfigured project.
        /// </summary>
        Task<string> GetProjectXml(UnconfiguredProject unconfiguredProject);

        /// <summary>
        /// Runs a given task inside either a read lock or a write lock.
        /// </summary>
        Task RunLocked(bool writeLock, Func<Task> task);
    }

    [Export(typeof(IMsBuildAccessor))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class MsBuildAccessor : IMsBuildAccessor
    {
        private readonly IProjectLockService _projectLockService;

        [ImportingConstructor]
        public MsBuildAccessor(IProjectLockService projectLockService)
        {
            _projectLockService = projectLockService;
        }

        public async Task<string> GetProjectXml(UnconfiguredProject unconfiguredProject)
        {
            var configuredProject = await unconfiguredProject.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
            using (var access = await _projectLockService.ReadLockAsync())
            {
                var xmlProject = await access.GetProjectAsync(configuredProject).ConfigureAwait(true);
                return xmlProject.Xml.RawXml;
            }
        }

        public async Task RunLocked(bool writeLock, Func<Task> task)
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
    }
}
