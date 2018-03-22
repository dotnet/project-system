// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     An <see langword="abstract"/> base class that loads, or unloads its inner instance when a 
    ///     <see cref="ConfiguredProject"/> becomes implicitly activated, or deactivated, respectively.
    /// </summary>
    internal abstract class AbstractImplicitlyActiveComponent : AbstractProjectDynamicLoadComponent
    {
        private readonly IConfiguredProjectImplicitActivationTracking _activationTracking;

        protected AbstractImplicitlyActiveComponent(IConfiguredProjectImplicitActivationTracking activationTracking, 
                                                 JoinableTaskContextNode joinableTaskContextNode)
            : base(joinableTaskContextNode)
        {
            Requires.NotNull(activationTracking, nameof(activationTracking));

            _activationTracking = activationTracking;
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _activationTracking.ImplicitlyActivated += OnImplicitlyActivated;
            _activationTracking.ImplicitlyDeactivated += OnImplicitlyDeactivated;

            if (_activationTracking.IsImplicitlyActive)
                return LoadAsync();

            return Task.CompletedTask;
        }

        private Task OnImplicitlyActivated(object sender, EventArgs args)
        {
            return LoadAsync();
        }

        private Task OnImplicitlyDeactivated(object sender, EventArgs args)
        {
            return UnloadAsync();
        }
    }
}
