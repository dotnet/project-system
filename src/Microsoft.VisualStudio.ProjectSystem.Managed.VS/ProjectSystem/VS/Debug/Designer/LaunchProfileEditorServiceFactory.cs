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
    [Export(typeof(IPackageService))]
    internal sealed class LaunchProfileEditorServiceFactory : IPackageService, IDisposable
    {
        private const string LaunchProfileEditorServiceName = "Microsoft.VisualStudio.ProjectSystem.Managed.LaunchProfileEditorService";
        private const string ProjectGuidActivationArgumentName = "ProjectGuid";

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

                if (options.ActivationArguments is null
                    || !options.ActivationArguments.TryGetValue("ProjectGuid", out string projectGuidString))
                {
                    throw new InvalidOperationException($"Missing required activation argument \"{ProjectGuidActivationArgumentName}\".");
                }

                if (!Guid.TryParse(projectGuidString, out Guid projectGuid))
                {
                    throw new InvalidOperationException($"Unable to parse activation argument \"{ProjectGuidActivationArgumentName}\": \"{projectGuidString}\".");
                }

                if (options.ClientRpcTarget is not ILaunchProfileEditorClientSession clientSession)
                {
                    throw new InvalidOperationException($"Missing client RPC target \"{nameof(ILaunchProfileEditorClientSession)}\".");
                }

                if (_launchProfileEditorService is null)
                {
                    _launchProfileEditorService = _projectServiceAccessor
                        .GetProjectService()
                        .Services
                        .ExportProvider
                        .GetExport<ILaunchProfileEditorService>()
                        .Value;
                }

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
