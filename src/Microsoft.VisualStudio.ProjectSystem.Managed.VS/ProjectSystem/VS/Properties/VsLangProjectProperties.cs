// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    internal class VsLangProjectProperties : VSProject, VSLangProj.ProjectProperties
    {
        private readonly VSProject _vsProject;
        private readonly ProjectProperties _projectProperties;

        public VsLangProjectProperties(
            VSProject vsProject,
            ProjectProperties projectProperties)
        {
            _vsProject = vsProject;
            _projectProperties = projectProperties;
        }

        // Implementation of VSProject to redirect the call to the actual VSProject object
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

        public VSLangProj.References References => _vsProject.References;

        public BuildManager BuildManager => _vsProject.BuildManager;

        public DTE DTE => _vsProject.DTE;

        public Project Project => _vsProject.Project;

        public ProjectItem WebReferencesFolder => _vsProject.WebReferencesFolder;

        public string TemplatePath => _vsProject.TemplatePath;

        public bool WorkOffline { get => _vsProject.WorkOffline; set => _vsProject.WorkOffline = value; }

        public Imports Imports => _vsProject.Imports;

        public VSProjectEvents Events => _vsProject.Events;

        // Implementation of VsLangProj.ProjectProperties
        public string __id => throw new System.NotImplementedException();

        public object __project => throw new System.NotImplementedException();

        public string StartupObject { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public prjOutputType OutputType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string RootNamespace { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string AssemblyName { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string AssemblyOriginatorKeyFile { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string AssemblyKeyContainerName { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public prjOriginatorKeyMode AssemblyOriginatorKeyMode { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool DelaySign { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string WebServer => throw new System.NotImplementedException();

        public string WebServerVersion => throw new System.NotImplementedException();

        public string ServerExtensionsVersion => throw new System.NotImplementedException();

        public bool LinkRepair { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string OfflineURL => throw new System.NotImplementedException();

        public string FileSharePath { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string ActiveFileSharePath => throw new System.NotImplementedException();

        public prjWebAccessMethod WebAccessMethod { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public prjWebAccessMethod ActiveWebAccessMethod => throw new System.NotImplementedException();

        public prjScriptLanguage DefaultClientScript { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public prjTargetSchema DefaultTargetSchema { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public prjHTMLPageLayout DefaultHTMLPageLayout { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string FileName { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string FullPath => throw new System.NotImplementedException();

        public string LocalPath => throw new System.NotImplementedException();

        public string URL => throw new System.NotImplementedException();

        public ProjectConfigurationProperties ActiveConfigurationSettings => throw new System.NotImplementedException();

        public object get_Extender(string ExtenderName)
        {
            throw new System.NotImplementedException();
        }

        public object ExtenderNames => throw new System.NotImplementedException();

        public string ExtenderCATID => throw new System.NotImplementedException();

        public string ApplicationIcon { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public prjOptionStrict OptionStrict { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string ReferencePath { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string OutputFileName => throw new System.NotImplementedException();

        public string AbsoluteProjectDirectory => throw new System.NotImplementedException();

        public prjOptionExplicit OptionExplicit { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public prjCompare OptionCompare { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public prjProjectType ProjectType => throw new System.NotImplementedException();

        public string DefaultNamespace { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}
