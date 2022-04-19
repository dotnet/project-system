// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint;
using Microsoft.VisualStudio.ProjectSystem.VS.TempPE;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    /// <summary>
    /// Manages the portable executable (PE) files produced by running custom tools.
    /// </summary>
    [Export(typeof(BuildManager))]
    [Export(typeof(VSBuildManager))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [Order(Order.Default)]
    internal class VSBuildManager : ConnectionPointContainer,
                                    IEventSource<_dispBuildManagerEvents>,
                                    BuildManager,
                                    BuildManagerEvents
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="VSBuildManager"/> class.
        /// </summary>
        [ImportingConstructor]
        internal VSBuildManager(IProjectThreadingService threadingService, IUnconfiguredProjectCommonServices unconfiguredProjectServices)
        {
            AddEventSource(this);
            _threadingService = threadingService;
            _unconfiguredProjectServices = unconfiguredProjectServices;
            Project = new OrderPrecedenceImportCollection<VSLangProj.VSProject>(projectCapabilityCheckProvider: unconfiguredProjectServices.Project);
        }

        [ImportMany(ExportContractNames.VsTypes.VSProject)]
        internal OrderPrecedenceImportCollection<VSLangProj.VSProject> Project { get; }

        // This has to be a property import to prevent a circular dependency as the bridge imports this class in order to fire events
        [Import]
        internal Lazy<IDesignTimeInputsBuildManagerBridge, IAppliesToMetadataView>? DesignTimeInputsBuildManagerBridge { get; private set; }

        /// <summary>
        /// Occurs when a design time output moniker is deleted.
        /// </summary>
        public event _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler? DesignTimeOutputDeleted;

        /// <summary>
        /// Occurs when a design time output moniker is dirty
        /// </summary>
        public event _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler? DesignTimeOutputDirty;

        /// <summary>
        /// Gets the project of which the selected item is a part.
        /// </summary>
        public EnvDTE.Project? ContainingProject => Project.FirstOrDefault()?.Value.Project;

        /// <summary>
        /// Gets the top-level extensibility object.
        /// </summary>
        public EnvDTE.DTE? DTE => Project.FirstOrDefault()?.Value.DTE;

        /// <summary>
        /// Gets the immediate parent object of a given object.
        /// </summary>
        public object? Parent => Project.FirstOrDefault()?.Value;

        /// <summary>
        /// Gets the temporary portable executable (PE) monikers for a project.
        /// </summary>
        public object DesignTimeOutputMonikers
        {
            get
            {
                if (DesignTimeInputsBuildManagerBridge?.AppliesTo(_unconfiguredProjectServices.Project.Capabilities) == true)
                {
                    IDesignTimeInputsBuildManagerBridge bridge = DesignTimeInputsBuildManagerBridge.Value;

                    // We don't need to thread switch here because if the caller is on the UI thread then everything is fine
                    // and if the caller is on a background thread, switching us to the UI thread doesn't provide any guarantees to it.
                    // It would mean the bridges state can't change, but it only reads the state once, and thats not our responsibility anyway.
                    return _threadingService.ExecuteSynchronously(bridge.GetDesignTimeOutputMonikersAsync);
                }

                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Builds a temporary portable executable (PE) and returns its description in an XML string.
        /// </summary>
        public string BuildDesignTimeOutput(string bstrOutputMoniker)
        {
            if (DesignTimeInputsBuildManagerBridge?.AppliesTo(_unconfiguredProjectServices.Project.Capabilities) == true)
            {
                IDesignTimeInputsBuildManagerBridge bridge = DesignTimeInputsBuildManagerBridge.Value;

                // See comment above about why we don't need any thread switching here.
                return _threadingService.ExecuteSynchronously(() => bridge.BuildDesignTimeOutputAsync(bstrOutputMoniker));
            }

            throw new NotImplementedException();
        }

        void IEventSource<_dispBuildManagerEvents>.OnSinkAdded(_dispBuildManagerEvents sink)
        {
            DesignTimeOutputDeleted += new _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler(sink.DesignTimeOutputDeleted);
            DesignTimeOutputDirty += new _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler(sink.DesignTimeOutputDirty);
        }

        void IEventSource<_dispBuildManagerEvents>.OnSinkRemoved(_dispBuildManagerEvents sink)
        {
            DesignTimeOutputDeleted -= new _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler(sink.DesignTimeOutputDeleted);
            DesignTimeOutputDirty -= new _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler(sink.DesignTimeOutputDirty);
        }

        /// <summary>
        /// Occurs when a design time output moniker is deleted.
        /// </summary>
        internal virtual void OnDesignTimeOutputDeleted(string outputMoniker)
        {
            _threadingService.VerifyOnUIThread();

            DesignTimeOutputDeleted?.Invoke(outputMoniker);
        }

        /// <summary>
        /// Occurs when a design time output moniker is dirty.
        /// </summary>
        internal virtual void OnDesignTimeOutputDirty(string outputMoniker)
        {
            _threadingService.VerifyOnUIThread();

            DesignTimeOutputDirty?.Invoke(outputMoniker);
        }
    }
}
