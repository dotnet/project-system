using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal static class IMsBuildModelWatcherFactory
    {
        public static IMsBuildModelWatcher CreateInstance() => Mock.Of<IMsBuildModelWatcher>();
    }
}
