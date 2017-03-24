// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using VSLangProj;
using VSLangProj110;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    /// See <see cref="VsLangProjectPropertiesProvider"/> for more info.
    /// </summary>
    internal partial class VsLangProjectProperties : VSLangProj.ProjectProperties
    {
        private readonly VSProject _vsProject;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly ActiveConfiguredProject<ProjectProperties> _projectProperties;

        public VsLangProjectProperties(
            VSProject vsProject,
            IUnconfiguredProjectVsServices projectVsServices,
            ActiveConfiguredProject<ProjectProperties> projectProperties)
        {
            _vsProject = vsProject;
            _projectVsServices = projectVsServices;
            _projectProperties = projectProperties;
        }

        private void RunFuncTaskSynchronously<T>(Func<T, Task> asyncActionT, T value)
        {
            _projectVsServices.ThreadingService.JoinableTaskFactory.Run(
                async () =>
                {
                    await asyncActionT(value).ConfigureAwait(false);
                });
        }

        public prjOutputTypeEx OutputTypeEx
        {
            get
            {
                var configurationGeneralBrowseObjectProperties = _projectVsServices.ThreadingService.JoinableTaskFactory.Run(_projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                var value = (IEnumValue)_projectVsServices.ThreadingService.JoinableTaskFactory.Run(configurationGeneralBrowseObjectProperties.OutputTypeEx.GetValueAsync);
                return (prjOutputTypeEx)Convert.ToInt32(value.DisplayName);
            }

            set
            {
                var configurationGeneralBrowseObjectProperties = _projectVsServices.ThreadingService.JoinableTaskFactory.Run(_projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                RunFuncTaskSynchronously(configurationGeneralBrowseObjectProperties.OutputTypeEx.SetValueAsync, (object)value);
            }
        }

        // Implementation of VsLangProj.ProjectProperties
        public prjOutputType OutputType
        {
            get
            {
                var configurationGeneralBrowseObjectProperties = _projectVsServices.ThreadingService.JoinableTaskFactory.Run(_projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                var value = (IEnumValue)_projectVsServices.ThreadingService.JoinableTaskFactory.Run(configurationGeneralBrowseObjectProperties.OutputType.GetValueAsync);
                return (prjOutputType)Convert.ToInt32(value.DisplayName);
            }

            set
            {
                var configurationGeneralBrowseObjectProperties = _projectVsServices.ThreadingService.JoinableTaskFactory.Run(_projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                RunFuncTaskSynchronously(configurationGeneralBrowseObjectProperties.OutputType.SetValueAsync, (object)value);
            }
        }

        public string AssemblyName
        {
            get
            {
                var configurationGeneralProperties = _projectVsServices.ThreadingService.JoinableTaskFactory.Run(_projectProperties.Value.GetConfigurationGeneralPropertiesAsync);
                return _projectVsServices.ThreadingService.JoinableTaskFactory.Run(configurationGeneralProperties.AssemblyName.GetEvaluatedValueAtEndAsync);
            }

            set
            {
                var configurationGeneralProperties = _projectVsServices.ThreadingService.JoinableTaskFactory.Run(_projectProperties.Value.GetConfigurationGeneralPropertiesAsync);
                RunFuncTaskSynchronously(configurationGeneralProperties.AssemblyName.SetValueAsync, (object)value);
            }
        }

        public string FullPath
        {
            get
            {
                var configurationGeneralProperties = _projectVsServices.ThreadingService.JoinableTaskFactory.Run(_projectProperties.Value.GetConfigurationGeneralPropertiesAsync);
                return _projectVsServices.ThreadingService.JoinableTaskFactory.Run(configurationGeneralProperties.TargetPath.GetEvaluatedValueAtEndAsync);
            }
        }

        public string ExtenderCATID => null;

        public string AbsoluteProjectDirectory
        {
            get
            {
                var configurationGeneralBrowseObjectProperties = _projectVsServices.ThreadingService.JoinableTaskFactory.Run(_projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                return _projectVsServices.ThreadingService.JoinableTaskFactory.Run(configurationGeneralBrowseObjectProperties.FullPath.GetEvaluatedValueAtEndAsync);
            }
        }

        public string __id => throw new NotImplementedException();

        public object __project => throw new NotImplementedException();

        public string StartupObject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string RootNamespace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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

        public string LocalPath => throw new NotImplementedException();

        public string URL => throw new NotImplementedException();

        public ProjectConfigurationProperties ActiveConfigurationSettings => throw new NotImplementedException();

        public object get_Extender(string ExtenderName)
        {
            throw new NotImplementedException();
        }

        public object ExtenderNames => throw new NotImplementedException();

        public string ApplicationIcon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjOptionStrict OptionStrict { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string ReferencePath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string OutputFileName => throw new NotImplementedException();

        public prjOptionExplicit OptionExplicit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjCompare OptionCompare { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjProjectType ProjectType => throw new NotImplementedException();

        public string DefaultNamespace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
