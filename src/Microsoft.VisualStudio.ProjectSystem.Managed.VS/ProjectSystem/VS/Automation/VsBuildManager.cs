// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint;
using Microsoft.VisualStudio.ProjectSystem.VS.Interop;
using Microsoft.VisualStudio.Shell;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    /// <summary>
    /// Undocumented.
    /// </summary>
    [Export(typeof(BuildManager))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [Order(Order.Default)]
    internal class VsBuildManager : ConnectionPointContainer,
                                    IEventSource<_dispBuildManagerEvents>,
                                    BuildManager,
                                    BuildManagerEvents
    {
        private readonly VSLangProj.VSProject _vsProject;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsBuildManager"/> class.
        /// </summary>
        [ImportingConstructor]
        internal VsBuildManager(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject)
        {
            AddEventSource(this as IEventSource<_dispBuildManagerEvents>);
            _vsProject = vsProject;
        }

        #region _dispBuildManagerEvents_Event Members

        /// <summary>
        /// Undocumented.
        /// </summary>
        public event _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler DesignTimeOutputDeleted;

        /// <summary>
        /// Undocumented.
        /// </summary>
        public event _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler DesignTimeOutputDirty;

        #endregion

        /// <summary>
        /// Undocumented.
        /// </summary>
        public virtual EnvDTE.Project ContainingProject
        {
            get { return _vsProject.Project; }
        }

        /// <summary>
        /// Undocumented.
        /// </summary>
        public virtual EnvDTE.DTE DTE
        {
            get { return _vsProject.DTE; }
        }

        /// <summary>
        /// Undocumented.
        /// </summary>
        public virtual object DesignTimeOutputMonikers
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Undocumented.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual object Parent
        {
            get { throw new NotImplementedException(); }
        }

        #region BuildManager Members

        /// <summary>
        /// Undocumented.
        /// </summary>
        public virtual string BuildDesignTimeOutput(string bstrOutputMoniker)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEventSource<_dispBuildManagerEvents> Members

        /// <summary>
        /// Undocumented.
        /// </summary>
        void IEventSource<_dispBuildManagerEvents>.OnSinkAdded(_dispBuildManagerEvents sink)
        {
            DesignTimeOutputDeleted += new _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler(sink.DesignTimeOutputDeleted);
            DesignTimeOutputDirty += new _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler(sink.DesignTimeOutputDirty);
        }

        /// <summary>
        /// Undocumented.
        /// </summary>
        void IEventSource<_dispBuildManagerEvents>.OnSinkRemoved(_dispBuildManagerEvents sink)
        {
            DesignTimeOutputDeleted -= new _dispBuildManagerEvents_DesignTimeOutputDeletedEventHandler(sink.DesignTimeOutputDeleted);
            DesignTimeOutputDirty -= new _dispBuildManagerEvents_DesignTimeOutputDirtyEventHandler(sink.DesignTimeOutputDirty);
        }

        #endregion

        /// <summary>
        /// Undocumented.
        /// </summary>
        protected virtual void OnDesignTimeOutputDeleted(string outputMoniker)
        {
            DesignTimeOutputDeleted?.Invoke(outputMoniker);
        }

        /// <summary>
        /// Undocumented.
        /// </summary>
        protected virtual void OnDesignTimeOutputDirty(string outputMoniker)
        {
            DesignTimeOutputDirty?.Invoke(outputMoniker);
        }
    }
}
