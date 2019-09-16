// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     An indirection that sends design-time build results in the form of command-line arguments to the F# language-service.
    /// </summary>
    /// <remarks>
    ///     This indirection is needed because Microsoft.VisualStudio.ProjectSystem.FSharp does not have InternalsVisibleTo access to Roslyn.
    /// </remarks>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class CommandLineNotificationHandler : AbstractWorkspaceContextHandler, ICommandLineHandler
    {
        [ImportingConstructor]
        public CommandLineNotificationHandler(ConfiguredProject project)
        {
            CommandLineNotifications = new OrderPrecedenceImportCollection<Action<string, BuildOptions, BuildOptions>>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<Action<string, BuildOptions, BuildOptions>> CommandLineNotifications
        {
            get;
        }

        public void Handle(IComparable version, BuildOptions added, BuildOptions removed, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            foreach (Lazy<Action<string, BuildOptions, BuildOptions>, IOrderPrecedenceMetadataView> value in CommandLineNotifications)
            {
                value.Value(Context.BinOutputPath, added, removed);
            }
        }
    }
}
