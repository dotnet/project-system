// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Telemetry
{
    [Export(typeof(ITelemetryService))]
    [Export(typeof(IVsTelemetryService))]
    internal class VsTelemetryService : IVsTelemetryService, ITelemetryService
    {
        private const string EventPrefix = "vs/projectsystem/managed/";
        private const string PropertyPrefix = "VS.ProjectSystem.Managed.";
        private const string ProjectIdGuid = PropertyPrefix + "ProjectGuid";
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectCommonServices;

        private TelemetryEventCorrelation _telemetryEventCorrelation;

        [ImportingConstructor]
        public VsTelemetryService(IUnconfiguredProjectCommonServices unconfiguredProjectCommonServices)
        {
            _unconfiguredProjectCommonServices = unconfiguredProjectCommonServices;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
        internal async Task OnProjectFactoryCompletedAsync()
        {
            await CreateProjectAssetAsync().ConfigureAwait(false);
        }

        private async Task CreateProjectAssetAsync()
        {
            var configurationGeneralProperties = await _unconfiguredProjectCommonServices
                                                .ActiveConfiguredProjectProperties
                                                .GetConfigurationGeneralPropertiesAsync()
                                                .ConfigureAwait(false);

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
                                            _unconfiguredProjectCommonServices.Project.FullPath.ToString(),
                                            0,
                                            projectProperties);
        }

        public async Task PostEventAsync(string eventName, UnconfiguredProject unconfiguredProject, IUnconfiguredProjectCommonServices unconfiguredProjectCommonServices, IEnumerable<KeyValuePair<string, object>> properties)
        {
            Requires.NotNullOrEmpty(eventName, nameof(eventName));
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(unconfiguredProjectCommonServices, nameof(unconfiguredProjectCommonServices));
            Requires.NotNull(properties, nameof(properties));

            var telemetryEvent = new TelemetryEvent(eventName);
            if (!((IDisposableObservable)unconfiguredProject).IsDisposed)
            {
                try
                {
                    await unconfiguredProject.Services.ProjectAsynchronousTasks.LoadedProjectAsync(async () =>
                    {
                        var configurationGeneralProperties = await unconfiguredProjectCommonServices
                                                                .ActiveConfiguredProjectProperties
                                                                .GetConfigurationGeneralPropertiesAsync()
                                                                .ConfigureAwait(false);
                        if (Guid.TryParse((string)await configurationGeneralProperties.ProjectGuid.GetValueAsync().ConfigureAwait(false), out Guid guid))
                        {
                            telemetryEvent.Properties[ProjectIdGuid] = guid;
                        }
                    });
                }
                // Project is unloaded or object is disposed
                catch (OperationCanceledException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }

            foreach (var property in properties)
            {
                telemetryEvent.Properties.Add(property);
            }

            TelemetryService.DefaultSession.PostEvent(eventName);
        }

        public void PostProperty(string eventName, string propertyName, string propertyValue, UnconfiguredProject unconfiguredProject)
        {
            TelemetryEvent telemetryEvent = new TelemetryEvent(EventPrefix + eventName.ToLower());
            telemetryEvent.Properties.Add(BuildPropertyName(eventName, propertyName), propertyValue);
            TryCorrelateProject(telemetryEvent);
            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
        }

        public void PostProperties(string eventName, List<(string propertyName, string propertyValue)> properties, UnconfiguredProject unconfiguredProject)
        {
            TelemetryEvent telemetryEvent = new TelemetryEvent(EventPrefix + eventName.ToLower());
            foreach (var property in properties)
            {
                telemetryEvent.Properties.Add(BuildPropertyName(eventName, property.propertyName), property.propertyValue);
            }

            TryCorrelateProject(telemetryEvent);
            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
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
        private void TryCorrelateProject(TelemetryEvent telemetryEvent)
        {
            telemetryEvent.Correlate(_telemetryEventCorrelation);
        }
    }
}
