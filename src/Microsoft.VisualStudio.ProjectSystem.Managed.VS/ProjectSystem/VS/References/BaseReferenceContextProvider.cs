// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    /// <summary>
    /// Base reference context provider which abstract CPS overrides
    /// </summary>
    internal class BaseReferenceContextProvider : IVsReferenceManagerUserAsync
    {
        /// <summary>
        /// Lazy instance of the next handler in the chain.
        /// </summary>
        private readonly Lazy<Lazy<IVsReferenceManagerUserAsync, IVsReferenceManagerUserComponentMetadataView>> _nextHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseReferenceContextProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        public BaseReferenceContextProvider(ConfiguredProject configuredProject)
        {
            ConfiguredProject = configuredProject;
            VsReferenceManagerUsers = new OrderPrecedenceImportCollection<IVsReferenceManagerUserAsync, IVsReferenceManagerUserComponentMetadataView>(projectCapabilityCheckProvider: configuredProject);
            _nextHandler = new Lazy<Lazy<IVsReferenceManagerUserAsync, IVsReferenceManagerUserComponentMetadataView>>(() =>
            {
                Type provider = GetType();
                OrderAttribute order = provider.GetCustomAttribute<OrderAttribute>();
                ExportIVsReferenceManagerUserAsyncAttribute user = provider.GetCustomAttribute<ExportIVsReferenceManagerUserAsyncAttribute>();
                return VsReferenceManagerUsers.FirstOrDefault(
                        export =>
                            export.Metadata.OrderPrecedence < order.OrderPrecedence &&
                            export.Metadata.ProviderContextIdentifier == user.ProviderContextIdentifier);
            });
        }

        /// <summary>
        /// Gets the next handler in the chain.
        /// </summary>
        private IVsReferenceManagerUserAsync? NextHandler => _nextHandler.Value?.Value;

        /// <summary>
        /// Gets the collection of reference provider contexts that can handle individual reference type operations.
        /// </summary>
        [ImportMany]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF")]
        private OrderPrecedenceImportCollection<IVsReferenceManagerUserAsync, IVsReferenceManagerUserComponentMetadataView> VsReferenceManagerUsers { get; }

        /// <summary>
        /// Gets the configured project.
        /// </summary>
        protected ConfiguredProject ConfiguredProject { get; }

        #region IVsReferenceManagerUserAsync

        /// <summary>
        /// Returns a value indicating whether this provider should be activated.
        /// </summary>
        /// <returns>Value indicating whether this provider should be activated.</returns>
        public virtual bool IsApplicable()
        {
            // There should always be a "next" handler as the usage of this class is to override a base
            // provider. If there's no other handler in the chain mark the provider as not applicable.
            return NextHandler?.IsApplicable() ?? false;
        }

        /// <summary>
        /// Creates a populated provider context.
        /// </summary>
        /// <remarks>
        /// The caller is responsible to dispose of the result when its use is over.
        /// </remarks>
        /// <returns>
        /// A task whose result is the export life time context. The expected type of object here is <see cref="IVsReferenceProviderContext"/>.
        /// Returning Task&lt;ExportLifetimeContext&lt;object[]&gt;&gt; instead of Task&lt;ExportLifetimeContext&lt;IVsReferenceProviderContext[]&gt;&gt; is because IVsReferenceProviderContext is an embedded interop type,
        /// so it can't be used across assembly boundaries.
        /// </returns>
        public virtual Task<ExportLifetimeContext<object>> CreateProviderContextAsync()
        {
            Assumes.NotNull(NextHandler);
            return NextHandler.CreateProviderContextAsync();
        }

        /// <summary>
        /// Applies reference changes.
        /// </summary>
        /// <param name="operation">The add or remove operation as defined by <see cref="__VSREFERENCECHANGEOPERATION"/></param>
        /// <param name="changedContext"><see cref="IVsReferenceProviderContext"/> representing the references to change. The declaration uses object instead because IVsReferenceProviderContext is an embedded interop type,
        /// so it can't be used across assembly boundaries.</param>
        /// <returns>A task whose result changes the references.</returns>
        public virtual Task ChangeReferencesAsync(uint operation, object changedContext)
        {
            Assumes.NotNull(NextHandler);
            return NextHandler.ChangeReferencesAsync(operation, changedContext);
        }

        #endregion

        /// <summary>
        /// Contains the constants used for order precedence metadata on the exports of initialized provider contexts.
        /// </summary>
        /// <remarks>
        /// Higher numbers appear earlier in the Reference Manager.
        /// 
        /// These values are the values CPS uses for the tab order. This should be removed
        /// when those are exposed publicly and use those values directly.
        /// </remarks>
        protected static class ReferencePriority
        {
            internal const int Platform = 6000;
            internal const int Assembly = 5000;
            internal const int Project = 4000;
            internal const int SharedProject = 3000;
            internal const int Com = 2000;
            internal const int File = 1000;
        }
    }
}
