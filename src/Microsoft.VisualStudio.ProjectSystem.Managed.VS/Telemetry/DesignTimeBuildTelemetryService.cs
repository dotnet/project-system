using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(IDesignTimeBuildTelemetryService))]
    internal class DesignTimeBuildTelemetryService : IDesignTimeBuildTelemetryService
    {
        private static volatile int s_numberOfProjectsInDesignTimeBuildQueue = 0;
        private static volatile int s_numberOfProjectsInLanguageServiceQueue = 0;
        private static readonly object s_seenProjectLock = new object();
        private static List<string> s_seenProjects = new List<string>();

        private readonly ITelemetryService _telemetryService;

        [ImportingConstructor]
        public DesignTimeBuildTelemetryService(ITelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        /// <summary>
        /// When a new project is created by the project system this method is called.
        /// This indicates that a design-time build has been queued.
        /// </summary>
        public void OnDesignTimeBuildQueued()
        {
            if (Interlocked.Increment(ref s_numberOfProjectsInDesignTimeBuildQueue) == 1)
            {
                _telemetryService.PostEvent("Load/DesignTimeBuild/Start");
            }
        }

        /// <summary>
        /// A design-time build has completed.
        /// </summary>
        public void OnDesignTimeBuildCompleted(string fullPathToProject)
        {
            lock (s_seenProjectLock)
            {
                if (!s_seenProjects.Contains(fullPathToProject))
                {
                    s_numberOfProjectsInLanguageServiceQueue++;
                    s_seenProjects.Add(fullPathToProject);
                }
            }
        }

        /// <summary>
        /// Project system has finished notifying the language service of the project items.
        /// </summary>
        public void OnLanguageServicePopulated()
        {
            var numberOfProjectsInLanguageServiceQueue = Interlocked.Decrement(ref s_numberOfProjectsInLanguageServiceQueue);

            // Every design time build that has been queued has been handed off to the
            // Language service.
            if (s_seenProjects.Count == s_numberOfProjectsInDesignTimeBuildQueue &&
                numberOfProjectsInLanguageServiceQueue == 0)
            {
                _telemetryService.PostEvent("Load/DesignTimeBuild/End");
            }
        }
    }
}
