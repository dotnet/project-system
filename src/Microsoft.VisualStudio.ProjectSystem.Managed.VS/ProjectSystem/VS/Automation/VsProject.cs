// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    /// <summary>
    /// <see cref="VSProject"/> imports <see cref="VSLangProj.VSProject"/> provided by CPS
    /// and wraps it into an object that implements both <see cref="VSLangProj.VSProject"/> and
    /// <see cref="VSLangProj.ProjectProperties"/>. This enables us to provide
    /// ProjectProperties to the Project Property Pages and maintain Backward Compatibility.
    /// </summary>
    /// <remarks>
    /// This implementation of VSProject just redirects the VSProject call to the contained
    /// VSProject object imported from CPS
    /// </remarks>
    [Export(ExportContractNames.VsTypes.VSProject, typeof(VSLangProj.VSProject))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [Order(Order.Default)]
    public partial class VSProject : VSLangProj.VSProject, IConnectionPointContainer, IEventSource<IPropertyNotifySink>
    {
        private readonly VSLangProj.VSProject _vsProject;
        private readonly IProjectThreadingService _threadingService;
        private readonly IActiveConfiguredValue<ProjectProperties> _projectProperties;
        private readonly BuildManager _buildManager;

        [ImportingConstructor]
        internal VSProject(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            IProjectThreadingService threadingService,
            IActiveConfiguredValue<ProjectProperties> projectProperties,
            UnconfiguredProject project,
            BuildManager buildManager)
        {
            _vsProject = vsProject;
            _threadingService = threadingService;
            _projectProperties = projectProperties;
            _buildManager = buildManager;

            ImportsImpl = new OrderPrecedenceImportCollection<Imports>(projectCapabilityCheckProvider: project);
            VSProjectEventsImpl = new OrderPrecedenceImportCollection<VSProjectEvents>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        internal OrderPrecedenceImportCollection<Imports> ImportsImpl { get; set; }

        [ImportMany]
        internal OrderPrecedenceImportCollection<VSProjectEvents> VSProjectEventsImpl { get; set; }

        public VSLangProj.References References => _vsProject.References;

        public BuildManager BuildManager => _buildManager;

        public DTE DTE => _vsProject.DTE;

        public Project Project => _vsProject.Project;

        public ProjectItem WebReferencesFolder => _vsProject.WebReferencesFolder;

        public string TemplatePath => _vsProject.TemplatePath;

        public bool WorkOffline { get => _vsProject.WorkOffline; set => _vsProject.WorkOffline = value; }

        public Imports Imports => ImportsImpl.FirstOrDefault()?.Value ?? _vsProject.Imports;

        public VSProjectEvents Events => VSProjectEventsImpl.FirstOrDefault()?.Value ?? _vsProject.Events;

        public ProjectItem CreateWebReferencesFolder()
        {
            return _vsProject.CreateWebReferencesFolder();
        }

        public ProjectItem AddWebReference(string bstrUrl)
        {
            return _vsProject.AddWebReference(bstrUrl);
        }

        public void Refresh()
        {
            _vsProject.Refresh();
        }

        public void CopyProject(string bstrDestFolder, string bstrDestUNCPath, prjCopyProjectOption copyProjectOption, string bstrUsername, string bstrPassword)
        {
            _vsProject.CopyProject(bstrDestFolder, bstrDestUNCPath, copyProjectOption, bstrUsername, bstrPassword);
        }

        public void Exec(prjExecCommand command, int bSuppressUI, object varIn, out object pVarOut)
        {
            _vsProject.Exec(command, bSuppressUI, varIn, out pVarOut);
        }

        public void GenerateKeyPairFiles(string strPublicPrivateFile, string strPublicOnlyFile = "0")
        {
            _vsProject.GenerateKeyPairFiles(strPublicPrivateFile, strPublicOnlyFile);
        }

        public string GetUniqueFilename(object pDispatch, string bstrRoot, string bstrDesiredExt)
        {
            return _vsProject.GetUniqueFilename(pDispatch, bstrRoot, bstrDesiredExt);
        }

        #region IConnectionPointContainer

        public void EnumConnectionPoints(out IEnumConnectionPoints? ppEnum)
        {
            ppEnum = null;
            (_vsProject as IConnectionPointContainer)?.EnumConnectionPoints(out ppEnum);
        }

        public void FindConnectionPoint(ref Guid riid, out IConnectionPoint? ppCP)
        {
            ppCP = null;
            (_vsProject as IConnectionPointContainer)?.FindConnectionPoint(ref riid, out ppCP);
        }

        #endregion IConnectionPointContainer

        #region IEventSource<IPropertyNotifySink>

        public void OnSinkAdded(IPropertyNotifySink sink)
        {
            (_vsProject as IEventSource<IPropertyNotifySink>)?.OnSinkAdded(sink);
        }

        public void OnSinkRemoved(IPropertyNotifySink sink)
        {
            (_vsProject as IEventSource<IPropertyNotifySink>)?.OnSinkRemoved(sink);
        }

        #endregion IEventSource<IPropertyNotifySink>
    }
}
