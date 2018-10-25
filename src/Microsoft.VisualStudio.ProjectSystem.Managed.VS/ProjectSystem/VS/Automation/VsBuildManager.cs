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

        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsBuildManager"/> class.
        /// </summary>
        [ImportingConstructor]
        internal VsBuildManager(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            IUnconfiguredProjectCommonServices unconfiguredProjectServices)
        {
            AddEventSource(this as IEventSource<_dispBuildManagerEvents>);
            _vsProject = vsProject;
            _unconfiguredProjectServices = unconfiguredProjectServices;
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
                var project = _unconfiguredProjectServices.ActiveConfiguredProject;
                var ruleSource = project.Services.ProjectSubscription.ProjectRuleSource;
                var update = _unconfiguredProjectServices.ThreadingService.ExecuteSynchronously(() => ruleSource.GetLatestVersionAsync(project, new string[] { Compile.SchemaName }));
                var snapshot = update[Compile.SchemaName];

                var monikers = new List<string>();
                foreach (var item in snapshot.Items.Values)
                {
                    bool isLink = GetBooleanPropertyValue(item, Compile.LinkProperty);
                    bool designTime = GetBooleanPropertyValue(item, Compile.DesignTimeProperty);

                    if (!isLink && designTime)
                    {
                        if (item.TryGetValue(Compile.FullPathProperty, out string path))
                        {
                            monikers.Add(path);
                        }
                    }
                }
                
                return monikers.ToArray();

                bool GetBooleanPropertyValue(System.Collections.Immutable.IImmutableDictionary<string, string> item, string propertyName)
                {
                    return item.TryGetValue(propertyName, out string value) && StringComparers.PropertyValues.Equals(value, "true");
                }
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
            var languageName = _unconfiguredProjectServices.ThreadingService.ExecuteSynchronously(async () =>
            {
                var property = await _unconfiguredProjectServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync();
                return await property.LanguageServiceName.GetValueAsync();
            });

            // TODO: Once Roslyn work is complete:
            //
            //   var compiler = _serviceProvider.GetService(typeof(Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS.ITempPECompilerHost));
            //  (after importing a service provider in the constructor as: [Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider)
            //  (or maybe can just import a Lazy<ITempPECompilerHost> in the constructor?)
            //
            //   compiler.Compile(languageName ... );

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
