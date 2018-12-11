// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class NuGetUIThreadHelper
    {
        /// <summary>
        /// Initially it will be null and will be initialized to CPS JTF when there is CPS
        /// based project is being created.
        /// </summary>
        private static Lazy<JoinableTaskFactory> _joinableTaskFactory;

        /// <summary>
        /// Returns the static instance of JoinableTaskFactory set by SetJoinableTaskFactoryFromService.
        /// If this has not been set yet the shell JTF will be used.
        /// During MEF composition some components will immediately call into the thread helper before
        /// it can be initialized. For this reason we need to fall back to the default shell JTF
        /// to provide basic threading support.
        /// </summary>
        public static JoinableTaskFactory JoinableTaskFactory
        {
            get
            {
                return _joinableTaskFactory?.Value ?? GetThreadHelperJoinableTaskFactorySafe();
            }
        }

        /// <summary>
        /// Retrieve the CPS enabled JoinableTaskFactory for the current version of Visual Studio.
        /// This overrides the default VsTaskLibraryHelper.ServiceInstance JTF.
        /// </summary>
        public static void SetJoinableTaskFactoryFromService(IProjectServiceAccessor projectServiceAccessor)
        {
            if (projectServiceAccessor == null)
            {
                throw new ArgumentNullException(nameof(projectServiceAccessor));
            }

            if (_joinableTaskFactory == null)
            {
                _joinableTaskFactory = new Lazy<JoinableTaskFactory>(() =>
                {
                    // Use IProjectService for Visual Studio 2017
                    var projectService = projectServiceAccessor.GetProjectService();
                    return projectService.Services.ThreadingPolicy.JoinableTaskFactory;
                },
                // This option helps avoiding deadlocks caused by CPS trying to create ProjectServiceHost
                // PublicationOnly mode lets parallel threads execute value factory method without
                // being blocked on each other.
                // It is correct behavior in this case as the value factory provides the same value
                // each time it is called and Lazy is used just for caching the value for perf reasons.
                LazyThreadSafetyMode.PublicationOnly);
            }
        }

        /// <summary>
        /// Set a non-Visual Studio JTF. This is used for standalone mode.
        /// </summary>
        public static void SetCustomJoinableTaskFactory(Thread mainThread, SynchronizationContext synchronizationContext)
        {
            if (mainThread == null)
            {
                throw new ArgumentNullException(nameof(mainThread));
            }

            if (synchronizationContext == null)
            {
                throw new ArgumentNullException(nameof(synchronizationContext));
            }

            // This method is not thread-safe and does not have it to be
            // This is really just a test-hook to be used by test standalone UI and only 1 thread will call into this
            // And, note that this method throws, when running inside VS, and ThreadHelper.JoinableTaskContext is not null
            _joinableTaskFactory = new Lazy<JoinableTaskFactory>(() =>
            {
#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
                var joinableTaskContext = new JoinableTaskContext(mainThread, synchronizationContext);
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext
                return joinableTaskContext.Factory;
            });
        }

        public static void SetCustomJoinableTaskFactory(JoinableTaskFactory joinableTaskFactory)
        {
            Assumes.Present(joinableTaskFactory);

            // This is really just a test-hook
            _joinableTaskFactory = new Lazy<JoinableTaskFactory>(() => joinableTaskFactory);
        }

        private static JoinableTaskFactory GetThreadHelperJoinableTaskFactorySafe()
        {
            // Static getter ThreadHelper.JoinableTaskContext, throws NullReferenceException if VsTaskLibraryHelper.ServiceInstance is null
            // And, ThreadHelper.JoinableTaskContext is simply 'ThreadHelper.JoinableTaskContext?.Factory'. Hence, this helper
            return VsTaskLibraryHelper.ServiceInstance != null ? ThreadHelper.JoinableTaskFactory : null;
        }
    }
}
