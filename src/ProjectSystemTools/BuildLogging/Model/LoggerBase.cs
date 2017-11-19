using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    internal abstract class LoggerBase : ILogger
    {
        protected readonly BuildTableDataSource DataSource;

        public LoggerVerbosity Verbosity { get => LoggerVerbosity.Diagnostic; set { } }

        public string Parameters { get; set; }

        protected LoggerBase(BuildTableDataSource dataSource)
        {
            DataSource = dataSource;
        }

        public abstract void Initialize(IEventSource eventSource);

        public virtual void Shutdown()
        {
        }
    }
}
