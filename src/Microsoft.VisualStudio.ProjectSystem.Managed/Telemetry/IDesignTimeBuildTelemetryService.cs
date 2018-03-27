namespace Microsoft.VisualStudio.Telemetry
{
    internal interface IDesignTimeBuildTelemetryService
    {
        void OnDesignTimeBuildQueued();
        void OnDesignTimeBuildCompleted(string fullPathToProject);
        void OnLanguageServicePopulated();
    }
}
