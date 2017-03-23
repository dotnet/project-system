// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using VSLangProj;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using VSLangProj110;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    internal class VsLangProjectProperties : VSProject, VSLangProj.ProjectProperties
    {
        private readonly VSProject _vsProject;
        private readonly ActiveConfiguredProject<ProjectProperties> _projectProperties;

        public VsLangProjectProperties(
            VSProject vsProject,
            ActiveConfiguredProject<ProjectProperties> projectProperties)
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
        public string __id => throw new NotImplementedException();

        public object __project => throw new NotImplementedException();

        public string StartupObject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public prjOutputType OutputType
        {
            get
            {
                var configurationGeneralBrowseObjectProperties = RunFuncTaskTSynchronously(_projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                var value = (IEnumValue)RunFuncTaskTSynchronously(configurationGeneralBrowseObjectProperties.OutputType.GetValueAsync);
                return (prjOutputType)Convert.ToInt32(value.DisplayName);
            }

            set
            {
                var configurationGeneralBrowseObjectProperties = RunFuncTaskTSynchronously(_projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                RunFuncTaskSynchronously(configurationGeneralBrowseObjectProperties.OutputType.SetValueAsync, (object)value);
            }
        }

        private T RunFuncTaskTSynchronously<T>(Func<Task<T>> asyncFuncTaskT)
        {
            T t = default(T);
            ThreadHelper.JoinableTaskFactory.Run(
                async () =>
                {
                    t = await asyncFuncTaskT().ConfigureAwait(false);
                });

            return t;
        }


        private void RunFuncTaskSynchronously<T>(Func<T, System.Threading.Tasks.Task> asyncActionT, T value)
        {
            ThreadHelper.JoinableTaskFactory.Run(
                async () =>
                {
                    await asyncActionT(value).ConfigureAwait(false);
                });
        }


        public string RootNamespace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string AssemblyName
        {
            get
            {
                var configurationGeneralProperties = RunFuncTaskTSynchronously(_projectProperties.Value.GetConfigurationGeneralPropertiesAsync);
                return RunFuncTaskTSynchronously(configurationGeneralProperties.AssemblyName.GetEvaluatedValueAtEndAsync);
            }

            set
            {
                var configurationGeneralProperties = RunFuncTaskTSynchronously(_projectProperties.Value.GetConfigurationGeneralPropertiesAsync);
                RunFuncTaskSynchronously(configurationGeneralProperties.AssemblyName.SetValueAsync, (object)value);
            }
        }
        public string AssemblyOriginatorKeyFile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string AssemblyKeyContainerName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public prjOriginatorKeyMode AssemblyOriginatorKeyMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool DelaySign { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string WebServer => throw new NotImplementedException();

        public string WebServerVersion => throw new NotImplementedException();

        public string ServerExtensionsVersion => throw new NotImplementedException();

        public bool LinkRepair { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string OfflineURL => throw new NotImplementedException();

        public string FileSharePath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string ActiveFileSharePath => throw new NotImplementedException();

        public prjWebAccessMethod WebAccessMethod { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjWebAccessMethod ActiveWebAccessMethod => throw new NotImplementedException();

        public prjScriptLanguage DefaultClientScript { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public prjTargetSchema DefaultTargetSchema { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public prjHTMLPageLayout DefaultHTMLPageLayout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string FileName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string FullPath
        {
            get
            {
                var configurationGeneralProperties = RunFuncTaskTSynchronously(_projectProperties.Value.GetConfigurationGeneralPropertiesAsync);
                return RunFuncTaskTSynchronously(configurationGeneralProperties.TargetPath.GetEvaluatedValueAtEndAsync);
            }
        }

        public string LocalPath => throw new NotImplementedException();

        public string URL => throw new NotImplementedException();

        public ProjectConfigurationProperties ActiveConfigurationSettings => throw new NotImplementedException();

        public object get_Extender(string ExtenderName)
        {
            throw new NotImplementedException();
        }

        public object ExtenderNames => throw new NotImplementedException();

        public string ExtenderCATID => null;

        public string ApplicationIcon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public prjOptionStrict OptionStrict { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ReferencePath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string OutputFileName => throw new NotImplementedException();

        public string AbsoluteProjectDirectory
        {
            get
            {
                var configurationGeneralBrowseObjectProperties = RunFuncTaskTSynchronously(_projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                return RunFuncTaskTSynchronously(configurationGeneralBrowseObjectProperties.FullPath.GetEvaluatedValueAtEndAsync);
            }
        }

        public prjOptionExplicit OptionExplicit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public prjCompare OptionCompare { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjProjectType ProjectType => throw new NotImplementedException();

        public string DefaultNamespace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjOutputTypeEx OutputTypeEx
        {
            get
            {
                var configurationGeneralBrowseObjectProperties = RunFuncTaskTSynchronously(_projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                var value = (IEnumValue)RunFuncTaskTSynchronously(configurationGeneralBrowseObjectProperties.OutputTypeEx.GetValueAsync);
                return (prjOutputTypeEx)Convert.ToInt32(value.DisplayName);
            }

            set
            {
                var configurationGeneralBrowseObjectProperties = RunFuncTaskTSynchronously(_projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                RunFuncTaskSynchronously(configurationGeneralBrowseObjectProperties.OutputTypeEx.SetValueAsync, (object)value);
            }
        }
    }
}
