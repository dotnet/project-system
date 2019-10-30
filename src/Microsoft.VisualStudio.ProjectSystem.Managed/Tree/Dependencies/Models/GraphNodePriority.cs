namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    /// <summary>
    /// Priorities used to order graph nodes.
    /// Not applicable to top-level nodes.
    /// When two nodes have the same priority, alphabetical ordering is used to break the tie.
    /// </summary>
    internal static class GraphNodePriority
    {
        public const int DiagnosticsError = 100;
        public const int DiagnosticsWarning = 101;
        public const int UnresolvedReference = 110;
        public const int Project = 120;
        public const int Package = 130;
        public const int FrameworkAssembly = 140;
        public const int PackageAssembly = 150;
        public const int Analyzer = 160;
        public const int ComNodePriority = 170;
        public const int SdkNodePriority = 180;
        public const int FrameworkReference = 190;
    }
}
