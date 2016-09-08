using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// This service provides value for the properties defined in Unconfigured project
    /// </summary>
    public interface IUnconfiguredProjectPropertyProviderService
    {
        /// <summary>
        /// Provides the semicolon separated list of Target frameworks
        /// </summary>
        Task<string> GetTargetFrameworksAsync();
    }
}