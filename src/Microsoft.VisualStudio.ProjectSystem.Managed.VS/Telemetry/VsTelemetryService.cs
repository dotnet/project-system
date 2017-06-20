// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(ITelemetryService))]
    [Export(typeof(IVsTelemetryService))]
    internal class VsTelemetryService : IVsTelemetryService, ITelemetryService
    {
        private const string EventPrefix = "vs/projectsystem/managed/";
        private const string PropertyPrefix = "VS.ProjectSystem.Managed.";
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectCommonServices;
        private readonly object _lock =  new object();

        private TelemetryEventCorrelation _telemetryEventCorrelation;
        private bool _telemetryEventCorrelationInitialized;
        private Guid _projectGuid;

        [ImportingConstructor]
        public VsTelemetryService(IUnconfiguredProjectCommonServices unconfiguredProjectCommonServices)
        {
            Requires.NotNull(unconfiguredProjectCommonServices, nameof(unconfiguredProjectCommonServices));

            _unconfiguredProjectCommonServices = unconfiguredProjectCommonServices;
        }

        /// <summary>
        /// Creates the Project Correlation asset which will be used to correlate all the telemetry events for this project
        /// identified uniquely through the Project Guid.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> CreateProjectCorrelationAssetAsync()
        {
            var configurationGeneralProperties = await _unconfiguredProjectCommonServices
                                                .ActiveConfiguredProjectProperties
                                                .GetConfigurationGeneralPropertiesAsync()
                                                .ConfigureAwait(false);

            if (Guid.TryParse((string)await configurationGeneralProperties.ProjectGuid.GetValueAsync().ConfigureAwait(false), out Guid guid))
            {
                _projectGuid = guid;
            }
            else
            {
                return false;
            }

            var targetFrameworkProperty = await configurationGeneralProperties.TargetFramework.GetEvaluatedValueAtEndAsync().ConfigureAwait(false);
            var targetFrameworksProperty = await configurationGeneralProperties.TargetFrameworks.GetEvaluatedValueAtEndAsync().ConfigureAwait(false);
            var outputTypeProperty = await configurationGeneralProperties.OutputType.GetEvaluatedValueAtEndAsync().ConfigureAwait(false);

            var eventName = "Project";
            var projectProperties = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(targetFrameworkProperty))
            {
                projectProperties[BuildPropertyName(eventName, ConfigurationGeneral.TargetFrameworkProperty)] = targetFrameworkProperty;
            }

            if (!string.IsNullOrEmpty(targetFrameworksProperty))
            {
                projectProperties[BuildPropertyName(eventName, ConfigurationGeneral.TargetFrameworksProperty)] = targetFrameworksProperty;
            }

            if (!string.IsNullOrEmpty(outputTypeProperty))
            {
                projectProperties[BuildPropertyName(eventName, ConfigurationGeneral.OutputTypeProperty)] = outputTypeProperty;
            }

            _telemetryEventCorrelation = TelemetryService.DefaultSession.PostAsset(
                                            EventPrefix + eventName.ToLowerInvariant(),
                                            _projectGuid.ToString(),
                                            0,
                                            projectProperties);
            _telemetryEventCorrelationInitialized = true;
            return true;
        }

        /// <summary>
        /// Posts a simple event with just the event name.
        /// </summary>
        /// <param name="eventName"></param>
        public void PostEvent(string eventName)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));

            TelemetryService.DefaultSession.PostEvent(eventName);
        }

        /// <summary>
        /// Post an event with the event name also with the corresponding Property name and Property value. This 
        /// event will be correlated with the Project Telemetry Correlation asset.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="propertyName">Property name to be reported.</param>
        /// <param name="propertyValue">Property value to be reported.</param>
        public void PostProperty(string eventName, string propertyName, string propertyValue)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));
            Requires.NotNullOrEmpty(propertyValue, nameof(propertyValue));

            TelemetryEvent telemetryEvent = new TelemetryEvent(EventPrefix + eventName.ToLower());
            telemetryEvent.Properties.Add(BuildPropertyName(eventName, propertyName), propertyValue);
            PostEvent(telemetryEvent);
        }

        /// <summary>
        /// Post an event with the event name also with the corresponding Property names and Property values. This 
        /// event will be correlated with the Project Telemetry Correlation asset.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="properties">List of Property name and corresponding values. PropertyName and PropertyValue cannot be null or empty.</param>
        public void PostProperties(string eventName, List<(string propertyName, string propertyValue)> properties)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNullOrEmpty(properties, nameof(properties));

            TelemetryEvent telemetryEvent = new TelemetryEvent(EventPrefix + eventName.ToLower());
            foreach (var property in properties)
            {
                telemetryEvent.Properties.Add(BuildPropertyName(eventName, property.propertyName), property.propertyValue);
            }

            PostEvent(telemetryEvent);
        }

        public TelemetryEventCorrelation PostOperation(string eventName, TelemetryResult result, string resultSummary = null, TelemetryEventCorrelation[] correlatedWith = null)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            if (result == TelemetryResult.None)
                throw new ArgumentException(null, nameof(result));

            return TelemetryService.DefaultSession.PostOperation(
                eventName: eventName,
                result: result,
                resultSummary: resultSummary,
                correlatedWith: correlatedWith);
        }

        public TelemetryEventCorrelation Report(string eventName, string description, Exception exception, Func<IFaultUtility, int> callback = null)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNull(exception, nameof(exception));

            return TelemetryService.DefaultSession.PostFault(
                eventName: eventName,
                description: description,
                exceptionObject: exception,
                gatherEventDetails: callback);
        }

        /// <summary>
        /// Hashes personally identifiable information for telemetry consumption.
        /// </summary>
        /// <param name="value">Value to hashed.</param>
        /// <returns>Hashed value.</returns>
        public string HashValue(string value)
        {
            using (var crypto = SHA256.Create())
            {
                byte[] hash = crypto.ComputeHash(Encoding.UTF8.GetBytes(value));
                return BitConverter.ToString(hash);
            }
        }


        /// <summary>
        /// Build a fully qualified property name based on it's parent event and the property name
        /// </summary>
        /// <param name="eventName">Name of the parent event.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Fully qualified property name.</returns>
        /// <remarks>
        /// Properties are expected to be in the following format - EventName.PropertyName
        /// with the the slashes from the vent name replaced by periods.
        /// e.g. vs/myevent would translate to VS.MyEvent.MyProperty
        /// </remarks>
        private string BuildPropertyName(string eventName, string propertyName)
        {
            string name = PropertyPrefix + eventName.Replace('/', '.');

            if (!name.EndsWith("."))
            {
                name += ".";
            }

            name += propertyName;

            return name;
        }

        /// <summary>
        /// Tries to correlate a project 
        /// </summary>
        /// <param name="telemetryEvent">Telemetry event to correlate the asset to.</param>
        private void PostEvent(TelemetryEvent telemetryEvent)
        {
            if (!_telemetryEventCorrelationInitialized)
            {
                lock (_lock)
                {
                    // Make sure it was not initialized while waiting for the lock
                    if (!_telemetryEventCorrelationInitialized)
                    {
                        _unconfiguredProjectCommonServices.ThreadingService.Fork(async () =>
                        {
                            if(await CreateProjectCorrelationAssetAsync().ConfigureAwait(false))
                            {
                                PostEvent(telemetryEvent, true);
                            }
                            else
                            {
                                PostEvent(telemetryEvent, false);
                            }
                        }, unconfiguredProject: _unconfiguredProjectCommonServices.Project);
                    }
                    else
                    {
                        PostEvent(telemetryEvent, true);
                    }
                }
            }
            else
            {
                PostEvent(telemetryEvent, true);
            }
        }

        /// <summary>
        /// Tries to correlate a project 
        /// </summary>
        /// <param name="telemetryEvent">Telemetry event to correlate the asset to.</param>
        /// <param name="correlate">Says whether the telemetry event should be correlated</param>
        private void PostEvent(TelemetryEvent telemetryEvent, bool correlate)
        {
            if (correlate)
            {
                telemetryEvent.Correlate(_telemetryEventCorrelation);
            }

            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
        }
    }
}
