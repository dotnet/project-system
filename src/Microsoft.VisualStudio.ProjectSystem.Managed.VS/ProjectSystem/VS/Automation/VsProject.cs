// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using EnvDTE;
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
    [Order(10)]
    public partial class VSProject : VSLangProj.VSProject
    {
        private readonly VSLangProj.VSProject _vsProject;
        private readonly IProjectThreadingService _threadingService;
        private readonly ActiveConfiguredProject<ProjectProperties> _projectProperties;

        [ImportingConstructor]
        internal VSProject(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            IProjectThreadingService threadingService,
            ActiveConfiguredProject<ProjectProperties> projectProperties)
        {
            Requires.NotNull(vsProject, nameof(vsProject));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(projectProperties, nameof(projectProperties));

            _vsProject = vsProject;
            _threadingService = threadingService;
            _projectProperties = projectProperties;
        }

        public VSLangProj.References References => _vsProject.References;

        public BuildManager BuildManager => _vsProject.BuildManager;

        public DTE DTE => _vsProject.DTE;

        public Project Project => _vsProject.Project;

        public ProjectItem WebReferencesFolder => _vsProject.WebReferencesFolder;

        public string TemplatePath => _vsProject.TemplatePath;

        public bool WorkOffline { get => _vsProject.WorkOffline; set => _vsProject.WorkOffline = value; }

        public Imports Imports => _vsProject.Imports;

        public VSProjectEvents Events => _vsProject.Events;

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
    }
}
