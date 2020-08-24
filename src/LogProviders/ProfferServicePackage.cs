// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts;
using ProvideBrokeredServiceAttribute = Microsoft.VisualStudio.Shell.ServiceBroker.ProvideBrokeredServiceAttribute;
using Microsoft.VisualStudio.ComponentModelHost;

namespace Microsoft.VisualStudio.ProjectSystem.Tools
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("E6C038D4-819B-41B7-95E1-BF748480B4BB")]
    [ProvideBrokeredService("LoggerService", "1.0", Audience = ServiceAudience.AllClientsIncludingGuests)]
    public class ProfferServicePackage : AsyncPackage
    {
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IComponentModel componentModel = (IComponentModel)await GetServiceAsync(typeof(SComponentModel));
            Assumes.Present(componentModel);
            IBuildLoggerService loggerService = componentModel.GetService<IBuildLoggerService>();

            IBrokeredServiceContainer brokeredServiceContainer = await this.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
            brokeredServiceContainer.Proffer(RpcDescriptors.LoggerServiceDescriptor, (mk, options, sb, ct) => new ValueTask<object>(loggerService));
        }
    }
}
