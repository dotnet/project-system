// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    ///     Provides an implementation of <see cref="IConfiguredProjectReadyToBuild"/> that allows
    ///     implicitly active <see cref="ConfiguredProject"/> objects to perform design-time builds.
    /// </summary>
    [Export(typeof(IConfiguredProjectReadyToBuild))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
    [Order(Order.Default)]
    internal sealed class ImplicitlyActiveConfiguredProjectReadyToBuild : IConfiguredProjectReadyToBuild
    {
        private readonly IConfiguredProjectImplicitActivationTracking _implicitActivationTracking;

        [ImportingConstructor]
        public ImplicitlyActiveConfiguredProjectReadyToBuild(IConfiguredProjectImplicitActivationTracking implicitActivationTracking)
        {
            _implicitActivationTracking = implicitActivationTracking;
        }

        public bool IsValidToBuild => _implicitActivationTracking.IsImplicitlyActive;

        public Task WaitReadyToBuildAsync() => _implicitActivationTracking.IsImplicitlyActiveTask;
    }
}
