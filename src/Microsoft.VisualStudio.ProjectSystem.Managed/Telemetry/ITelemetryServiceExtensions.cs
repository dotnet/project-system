using System.Collections.Generic;

namespace Microsoft.VisualStudio.Telemetry
{
    /// <summary>
    /// There are cases where the Telemetry Service will not be available. This happens when the VS layer of the project system
    /// is not part of the application. These extensions provide a safe way to call the service without checking the presence or 
    /// absence of the service.
    /// </summary>
    internal static class ITelemetryServiceExtensions
    {
        public static string HashValueSafe(this ITelemetryService service, string value)
        {
            return service?.HashValue(value) ?? value;
        }

        public static void PostPropertiesSafe(this ITelemetryService service, string eventName, List<(string propertyName, string propertyValue)> properties)
        {
            service?.PostProperties(eventName, properties);
        }

        public static void PostPropertySafe(this ITelemetryService service, string eventName, string propertyName, string propertyValue)
        {
            service?.PostProperty(eventName, propertyName, propertyValue);
        }
    }
}
