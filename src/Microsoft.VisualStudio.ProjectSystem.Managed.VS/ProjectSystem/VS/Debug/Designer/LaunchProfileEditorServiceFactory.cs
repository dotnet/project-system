// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Microsoft.VisualStudio.ProjectSystem.Debug.Designer;
using Microsoft.VisualStudio.RpcContracts;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug.Designer
{
    /// <summary>
    /// Reponsible for proffering the <see cref="ILaunchProfileEditorService"/> through
    /// the <see cref="IServiceBroker"/>.
    /// </summary>
    [Export(typeof(IPackageService))]
    internal sealed class LaunchProfileEditorServiceFactory : IPackageService, IDisposable
    {
        private const string LaunchProfileEditorServiceName = "Microsoft.VisualStudio.ProjectSystem.Managed.LaunchProfileEditorService";
        private const string ProjectGuidActivationArgumentName = "ProjectGuid";

        /// <summary>
        /// The actual service descriptor the client UI would use to request the service.
        /// </summary>
        internal static readonly ServiceJsonRpcDescriptor LaunchProfileEditorServiceDescriptorV1 = new(
            new ServiceMoniker(LaunchProfileEditorServiceName, new Version(0, 1)),
            ServiceJsonRpcDescriptor.Formatters.UTF8,
            ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);

        private readonly IProjectServiceAccessor _projectServiceAccessor;
        private IDisposable? _profferDisposable;
        private ILaunchProfileEditorService? _launchProfileEditorService;

        [ImportingConstructor]
        public LaunchProfileEditorServiceFactory(IProjectServiceAccessor projectServiceAccessor)
        {
            _projectServiceAccessor = projectServiceAccessor;
        }

        public async Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            IBrokeredServiceContainer brokeredServiceContainer = await asyncServiceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();

            _profferDisposable = brokeredServiceContainer.Proffer(LaunchProfileEditorServiceDescriptorV1, CreateInstanceAsync);

            async ValueTask<object?> CreateInstanceAsync(
                ServiceMoniker moniker,
                ServiceActivationOptions options,
                IServiceBroker serviceBroker,
                AuthorizationServiceClient authorizationServiceClient,
                CancellationToken cancellationToken)
            {
                await authorizationServiceClient.AuthorizeOrThrowAsync(WellKnownProtectedOperations.CreateClientIsOwner(), cancellationToken);

                // When the client requests the service we expect it to pass the target project's
                // GUID, which is how we figure out which list of launch profiles to provide.
                if (options.ActivationArguments is null
                    || !options.ActivationArguments.TryGetValue(ProjectGuidActivationArgumentName, out string projectGuidString))
                {
                    throw new InvalidOperationException($"Missing required activation argument \"{ProjectGuidActivationArgumentName}\".");
                }

                if (!Guid.TryParse(projectGuidString, out Guid projectGuid))
                {
                    throw new InvalidOperationException($"Unable to parse activation argument \"{ProjectGuidActivationArgumentName}\": \"{projectGuidString}\".");
                }

                // We also expect the client to provide an ILaunchProfileEditorClientSession; this
                // is the interface we use when we want to communicate information back to the
                // client (e.g., the list of launch profiles, updates to properties, etc.).
                if (options.ClientRpcTarget is not ILaunchProfileEditorClientSession clientSession)
                {
                    throw new InvalidOperationException($"Missing client RPC target \"{nameof(ILaunchProfileEditorClientSession)}\".");
                }

                // Obtain the MEF-exported ILaunchProfileEditorService so we can delegate to that.
                if (_launchProfileEditorService is null)
                {
                    _launchProfileEditorService = _projectServiceAccessor
                        .GetProjectService()
                        .Services
                        .ExportProvider
                        .GetExport<ILaunchProfileEditorService>()
                        .Value;
                }

                // Create the new ILaunchProfileEditorConnection that we pass back to the client,
                // so the client can add/remove/update the launch profiles.
                if (await _launchProfileEditorService.CreateConnectionAsync(projectGuid, clientSession) is not ILaunchProfileEditorConnection connection)
                {
                    throw new InvalidOperationException($"Unable to connect to project with GUID \"{projectGuid}\".");
                }

                return connection;
            }
        }

        public void Dispose()
        {
            _profferDisposable?.Dispose();
        }
    }
}
