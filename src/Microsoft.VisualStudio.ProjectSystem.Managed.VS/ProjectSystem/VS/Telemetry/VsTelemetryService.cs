// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Telemetry;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Telemetry
{
    [Export(typeof(ITelemetryService))]
    [SuppressMessage("Microsoft.Cryptographic.Standard", "CA5350:MD5CannotBeUsed", Justification = "MD5 hash is not being used for security")]
    internal class VsTelemetryService : ITelemetryService
    {
        /// <summary>
        /// Prefix for events.
        /// </summary>
        private const string EventPrefix = "vs/projectsystem/managed/";

        /// <summary>
        /// Dictionary of known project assets.
        /// </summary>
        private Dictionary<string, TelemetryEventCorrelation> projectAssets = new Dictionary<string, TelemetryEventCorrelation>();

        /// <summary>
        /// Project lock service.
        /// </summary>
        private readonly IProjectLockService _projectLockService;

        private readonly IUnconfiguredProjectVsServices _projectVsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsTelemetryService"/> class.
        /// </summary>
        /// <param name="projectLockService">Project lock service.</param>
        /// <param name="projectVsService"></param>
        [ImportingConstructor]
        public VsTelemetryService(IProjectLockService projectLockService, IUnconfiguredProjectVsServices projectVsService)
        {
            _projectLockService = projectLockService;
            _projectVsService = projectVsService;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        internal async Task OnProjectFactoryCompletedAsync()
        {
            await CreateProjectAsset(_projectVsService.Project).ConfigureAwait(false);
        }

        /// <summary>
        /// Posts a given telemetry event path to the telemetry service session for the program.
        /// </summary>
        /// <param name="telemetryEvent">Name of the event to post.</param>
        public void PostEvent(string telemetryEvent)
        {
            TelemetryService.DefaultSession.PostEvent($"{EventPrefix}{telemetryEvent}");
        }

        /// <summary>
        /// Posts a given telemetry operation to the telemetry service. <seealso cref="TelemetrySessionExtensions.PostOperation(TelemetrySession, string, TelemetryResult, string, TelemetryEventCorrelation[])"/>
        /// </summary>
        /// <param name="operationPath">Postfix on "vs/projectsystem/"</param>
        /// <param name="result">The result of the operation</param>
        /// <param name="resultSummary">Summary of the result</param>
        /// <param name="correlatedWith">Events to correlate this event with</param>
        /// <returns>Posted telemetry event.</returns>
        public TelemetryEventCorrelation PostOperation(string operationPath, TelemetryResult result, string resultSummary = null, TelemetryEventCorrelation[] correlatedWith = null)
        {
            return TelemetryService.DefaultSession.PostOperation(
                eventName: $"{EventPrefix}{operationPath}",
                result: result,
                resultSummary: resultSummary,
                correlatedWith: correlatedWith);
        }

        /// <summary>
        /// Reports a given non-fatal watson to the telemetry service. <seealso cref="TelemetrySessionExtensions.PostFault(TelemetrySession, string, string, Exception, Func{IFaultUtility, int}, TelemetryEventCorrelation[])"/>
        /// </summary>
        /// <param name="eventPostfix">Postfix on "vs/projectsystem/"</param>
        /// <param name="description">Description to include with the NFW</param>
        /// <param name="exception">Exception that caused the NFW</param>
        /// <param name="callback">Gathers information for the NFW</param>
        /// <returns>Posted telemetry event.</returns>
        public TelemetryEventCorrelation Report(string eventPostfix, string description, Exception exception, Func<IFaultUtility, int> callback = null)
        {
            return TelemetryService.DefaultSession.PostFault(
                eventName: $"{EventPrefix}{eventPostfix}",
                description: description,
                exceptionObject: exception,
                gatherEventDetails: callback);
        }

        /// <summary>
        /// Posts an event with a single property.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="propertyValue">Value of the property.</param>
        /// <param name="unconfiguredProject">Correlated project asset for this event.</param>
        public void PostProperty(string eventName, string propertyName, string propertyValue, UnconfiguredProject unconfiguredProject = null)
        {
            TelemetryEvent telemetryEvent = new TelemetryEvent(EventPrefix + eventName.ToLower());
            telemetryEvent.Properties.Add(BuildPropertyName(eventName, propertyName), propertyValue);
            TryCorrelateProject(unconfiguredProject, telemetryEvent);
            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
        }

        /// <summary>
        /// Posts an event with multiple properties.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="properties">Collection of property name and values.</param>
        /// <param name="unconfiguredProject">Correlated project asset for this event.</param>
        public void PostProperties(string eventName, IEnumerable<(string propertyName, string propertyValue)> properties, UnconfiguredProject unconfiguredProject = null)
        {
            TelemetryEvent telemetryEvent = new TelemetryEvent(EventPrefix + eventName.ToLower());
            foreach(var property in properties)
            {
                telemetryEvent.Properties.Add(BuildPropertyName(eventName, property.propertyName), property.propertyValue);
            }

            TryCorrelateProject(unconfiguredProject, telemetryEvent);
            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
        }

        /// <summary>
        /// Hashes personally identifiable information for telemetry consumption.
        /// </summary>
        /// <param name="value">Value to hashed.</param>
        /// <returns>Hashed value.</returns>
        public string HashValue(string value)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
                return BitConverter.ToString(hash);
            }
        }

        /// <summary>
        /// Creates a project asset.
        /// </summary>
        /// <param name="unconfiguredProject">Project to create the asset from.</param>
        /// <returns>Telemetry event representing the project asset.</returns>
        public async Task CreateProjectAsset(UnconfiguredProject unconfiguredProject)
        {
            string assetId = GetProjectId(unconfiguredProject);
            if (!projectAssets.ContainsKey(assetId))
            {
                Dictionary<string, object> projectProperties = new Dictionary<string, object>();
                string eventName = "Project";

                using (var access = await _projectLockService.ReadLockAsync())
                {
                    var configuredProject = await unconfiguredProject.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
                    var properties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();

                    List<string> propertyNames = new List<string>()
                    {
                        ConfigurationGeneral.TargetFrameworkProperty,
                        ConfigurationGeneral.TargetFrameworksProperty,
                        ConfigurationGeneral.OutputTypeProperty
                    };

                    foreach (var propertyName in propertyNames)
                    {
                        var propertyValue = await properties.GetEvaluatedPropertyValueAsync(propertyName).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(propertyValue))
                        {
                            projectProperties[BuildPropertyName(eventName, propertyName)] = propertyValue;
                        }
                    }
                }

                var projectAsset = TelemetryService.DefaultSession.PostAsset(
                    EventPrefix + eventName.ToLowerInvariant(),
                    assetId,
                    0,
                    projectProperties);

                projectAssets.Add(assetId, projectAsset);
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
            string name = "VS.ProjectSystem.Managed." + eventName.Replace('/', '.');

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
        /// <param name="unconfiguredProject">Correlated project asset for this event.</param>
        /// <param name="telemetryEvent">Telemetry event to correlate the asset to.</param>
        private void TryCorrelateProject(UnconfiguredProject unconfiguredProject, TelemetryEvent telemetryEvent)
        {
            if (unconfiguredProject != null && projectAssets.TryGetValue(GetProjectId(unconfiguredProject), out TelemetryEventCorrelation projectAsset))
            {
                telemetryEvent.Correlate(projectAsset);
            }
        }

        /// <summary>
        /// Gets an unique identifier for the unconfigured project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project.</param>
        /// <returns>Unique identifier for the project.</returns>
        private string GetProjectId(UnconfiguredProject unconfiguredProject)
        {
            // TODO: This should really be the project GUID. For now use a hash of the filename
            return HashValue(unconfiguredProject.FullPath.ToLowerInvariant());
        }
    }
}
