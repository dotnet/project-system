// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    /// <summary>
    /// Base reference context provider which abstract CPS overrides
    /// </summary>
    public class BaseReferenceContextProvider : IVsReferenceManagerUserAsync
    {
        /// <summary>
        /// Value used to override the CPS provider
        /// </summary>
        public const int OverrideCPSProvider = 1;

        /// <summary>
        /// Lazy instance of the next handler in the chain.
        /// </summary>
        private Lazy<Lazy<IVsReferenceManagerUserAsync, IVsReferenceManagerUserComponentMetadataView>> nextHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseReferenceContextProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        public BaseReferenceContextProvider(ConfiguredProject configuredProject)
        {
            this.VsReferenceManagerUsers = new OrderPrecedenceImportCollection<IVsReferenceManagerUserAsync, IVsReferenceManagerUserComponentMetadataView>(projectCapabilityCheckProvider: configuredProject);
            this.nextHandler = new Lazy<Lazy<IVsReferenceManagerUserAsync, IVsReferenceManagerUserComponentMetadataView>>(() =>
            {
                Type provider = this.GetType();
                var order = provider.GetCustomAttribute<OrderAttribute>();
                var user = provider.GetCustomAttribute<ExportIVsReferenceManagerUserAsyncAttribute>();
                return this.VsReferenceManagerUsers.FirstOrDefault(
                        export =>
                            export.Metadata.OrderPrecedence < order.OrderPrecedence &&
                            export.Metadata.ProviderContextIdentifier == user.ProviderContextIdentifier);
            });
        }

        /// <summary>
        /// Gets the next handler in the chain.
        /// </summary>
        private IVsReferenceManagerUserAsync NextHandler
        {
            get
            {
                return nextHandler?.Value?.Value;
            }
        }

        /// <summary>
        /// Gets the collection of reference provider contexts that can handle individual reference type operations.
        /// </summary>
        [ImportMany]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF")]
        private OrderPrecedenceImportCollection<IVsReferenceManagerUserAsync, IVsReferenceManagerUserComponentMetadataView> VsReferenceManagerUsers { get; set; }

        /// <summary>
        /// Gets the configured project.
        /// </summary>
        [Import]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF")]
        protected ConfiguredProject ConfiguredProject { get; private set; }

        #region IVsReferenceManagerUserAsync

        /// <summary>
        /// Returns a value indicating whether this provider should be activated.
        /// </summary>
        /// <returns>Value indicating whether this provider should be activated.</returns>
        public virtual bool IsApplicable()
        {
            // There should always be a "next" handler as the usage of this class is to override a base
            // provider. If there's no other handler in the chain mark the provider as not applicable.
            return this.NextHandler?.IsApplicable() ?? false;
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
            return this.NextHandler.CreateProviderContextAsync();
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
            return this.NextHandler.ChangeReferencesAsync(operation, changedContext);
        }

        #endregion

        /// <summary>
        /// Contains the constants used for order precedence metadata on the exports of initialized provider contexts.
        /// </summary>
        /// <remarks>
        /// Higher numbers appear earlier in the Reference Manager.
        /// 
        /// These values are the values CPS uses for the tab order. This should be removed
        /// when those are exposed publically and use those values directly.
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

        #region TODO: This should be deleted once the updated CPS package is published

        /// <summary>
        /// A view at metadata that is expected on an export of <see cref="IVsReferenceManagerUserAsync"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Vs", Justification = "Vs is a Visual Studio naming convention")]
        public interface IVsReferenceManagerUserComponentMetadataView : IOrderPrecedenceMetadataView
        {
            /// <summary>
            /// Gets the GUID that matches the reference manager context provider's GUID.
            /// </summary>
            string ProviderContextIdentifier { get; }

            /// <summary>
            /// Gets the number to control the order of the tab in the reference manager.
            /// If it is 0, the order of the component will be used.
            /// </summary>
            int Position { get; }
        }

        /// <summary>
        /// Exports <see cref="IVsReferenceManagerUserAsync"/> with relevant metadata.
        /// </summary>
        [MetadataAttribute]
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
        public sealed class ExportIVsReferenceManagerUserAsyncAttribute : ExportAttribute
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ExportIVsReferenceManagerUserAsyncAttribute"/> class.
            /// Code should switch to use <see cref="ExportIVsReferenceManagerUserAsyncAttribute.ExportIVsReferenceManagerUserAsyncAttribute(string, int)"/> instead of this one
            /// to include a valid position.
            /// </summary>
            /// <param name="providerContextIdentifier">The GUID that describes which reference manager provider context this export initializes.</param>
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public ExportIVsReferenceManagerUserAsyncAttribute(string providerContextIdentifier)
                : this(providerContextIdentifier, 0)
            {
                this.ProviderContextIdentifier = providerContextIdentifier;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ExportIVsReferenceManagerUserAsyncAttribute"/> class.
            /// </summary>
            /// <param name="providerContextIdentifier">The GUID that describes which reference manager provider context this export initializes.</param>
            /// <param name="position">The number that controls the order of tabs in the reference manager, a page with higher number appears earlier in the dialog.</param>
            public ExportIVsReferenceManagerUserAsyncAttribute(string providerContextIdentifier, int position)
                : base(typeof(IVsReferenceManagerUserAsync))
            {
                this.ProviderContextIdentifier = providerContextIdentifier;
                this.Position = position;
            }

            /// <summary>
            /// Gets the GUID that describes which reference manager provider context this export initializes.
            /// </summary>
            public string ProviderContextIdentifier { get; private set; }

            /// <summary>
            /// Gets the number controlling the order of the tab in the reference manager.
            /// If it is 0, the order number of the MEF component will be used.
            /// </summary>
            public int Position { get; }
        }

        #endregion
    }
}
