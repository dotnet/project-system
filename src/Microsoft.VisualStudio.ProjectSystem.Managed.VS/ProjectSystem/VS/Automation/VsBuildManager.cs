// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint;
using Microsoft.VisualStudio.ProjectSystem.VS.TempPE;

using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    /// <summary>
    /// Manages the portable executable (PE) files produced by running custom tools.
    /// </summary>
    [Export(typeof(BuildManager))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [Order(Order.Default)]
    internal class VSBuildManager : ConnectionPointContainer,
                                    IEventSource<_dispBuildManagerEvents>,
                                    BuildManager,
                                    BuildManagerEvents
    {
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly ITempPEBuildManager _tempPEManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="VSBuildManager"/> class.
        /// </summary>
        [ImportingConstructor]
        internal VSBuildManager(IUnconfiguredProjectCommonServices unconfiguredProjectServices,
            ITempPEBuildManager tempPEManager
            )
        {
            AddEventSource(this as IEventSource<_dispBuildManagerEvents>);
            _unconfiguredProjectServices = unconfiguredProjectServices;
            _tempPEManager = tempPEManager;
            Project = new OrderPrecedenceImportCollection<VSLangProj.VSProject>(projectCapabilityCheckProvider: _unconfiguredProjectServices.Project);
        }

        [ImportMany(ExportContractNames.VsTypes.VSProject)]
        internal OrderPrecedenceImportCollection<VSLangProj.VSProject> Project { get; set; }

        #region _dispBuildManagerEvents_Event Members

        /// <summary>
        /// Occurs when a design time output moniker is deleted.
        /// </summary>
        public event _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler DesignTimeOutputDeleted;

        /// <summary>
        /// Occurs when a design time output moniker is dirty
        /// </summary>
        public event _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler DesignTimeOutputDirty;

        #endregion

        /// <summary>
        /// Gets the project of which the selected item is a part.
        /// </summary>
        public EnvDTE.Project ContainingProject => Project.FirstOrDefault()?.Value.Project;

        /// <summary>
        /// Gets the top-level extensibility object.
        /// </summary>
        public EnvDTE.DTE DTE => Project.FirstOrDefault()?.Value.DTE;

        /// <summary>
        /// Gets the immediate parent object of a given object.
        /// </summary>
        public object Parent => Project.FirstOrDefault()?.Value;

        /// <summary>
        /// Gets the temporary portable executable (PE) monikers for a project.
        /// </summary>
        public object DesignTimeOutputMonikers
        {
            get
            {
                return _unconfiguredProjectServices.ThreadingService.ExecuteSynchronously(async () =>
                {
                    return await _tempPEManager.GetDesignTimeOutputFilenamesAsync(false);
                });
            }
        }

        #region BuildManager Members

        /// <summary>
        /// Builds a temporary portable executable (PE) and returns its description in an XML string.
        /// </summary>
        public string BuildDesignTimeOutput(string bstrOutputMoniker)
        {
            if (bstrOutputMoniker == null) return null;

            return _unconfiguredProjectServices.ThreadingService.ExecuteSynchronously(async () =>
            { 
                 return await _tempPEManager.GetTempPEBlobAsync(bstrOutputMoniker);
            });
        }

        #endregion

        #region IEventSource<_dispBuildManagerEvents> Members

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

        #endregion

        /// <summary>
        /// Occurs when a design time output moniker is deleted.
        /// </summary>
        protected void OnDesignTimeOutputDeleted(string outputMoniker)
        {
            DesignTimeOutputDeleted?.Invoke(outputMoniker);
        }

        /// <summary>
        /// Occurs when a design time output moniker is dirty.
        /// </summary>
        protected void OnDesignTimeOutputDirty(string outputMoniker)
        {
            DesignTimeOutputDirty?.Invoke(outputMoniker);
        }
    }
}
