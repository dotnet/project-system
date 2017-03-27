using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;
using VSLangProj;
using VSLangProj110;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    internal partial class VSProject : VSLangProj.ProjectProperties
    {

        private ProjectProperties ProjectProperties
        {
            get { return _projectProperties.Value; }
        }

        private JoinableTaskFactory JoinableTaskFactory
        {
            get { return _threadingService.JoinableTaskFactory; }
        }

        private void RunFuncTaskSynchronously<T>(Func<T, Task> asyncActionT, T value)
        {
            JoinableTaskFactory.Run(
                async () =>
                {
                    await asyncActionT(value).ConfigureAwait(false);
                });
        }

        public prjOutputTypeEx OutputTypeEx
        {
            get
            {
                var configurationGeneralBrowseObjectProperties = JoinableTaskFactory.Run(ProjectProperties.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                var value = (IEnumValue)JoinableTaskFactory.Run(configurationGeneralBrowseObjectProperties.OutputTypeEx.GetValueAsync);
                return (prjOutputTypeEx)Convert.ToInt32(value.DisplayName);
            }

            set
            {
                var configurationGeneralBrowseObjectProperties = JoinableTaskFactory.Run(ProjectProperties.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                RunFuncTaskSynchronously(configurationGeneralBrowseObjectProperties.OutputTypeEx.SetValueAsync, (object)value);
            }
        }


        // Implementation of VsLangProj.ProjectProperties
        public prjOutputType OutputType
        {
            get
            {
                var configurationGeneralBrowseObjectProperties = JoinableTaskFactory.Run(ProjectProperties.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                var value = (IEnumValue)JoinableTaskFactory.Run(configurationGeneralBrowseObjectProperties.OutputType.GetValueAsync);
                return (prjOutputType)Convert.ToInt32(value.DisplayName);
            }

            set
            {
                var configurationGeneralBrowseObjectProperties = JoinableTaskFactory.Run(ProjectProperties.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                RunFuncTaskSynchronously(configurationGeneralBrowseObjectProperties.OutputType.SetValueAsync, (object)value);
            }
        }

        public string AssemblyName
        {
            get
            {
                var configurationGeneralProperties = JoinableTaskFactory.Run(ProjectProperties.GetConfigurationGeneralPropertiesAsync);
                return JoinableTaskFactory.Run(configurationGeneralProperties.AssemblyName.GetEvaluatedValueAtEndAsync);
            }

            set
            {
                var configurationGeneralProperties = JoinableTaskFactory.Run(ProjectProperties.GetConfigurationGeneralPropertiesAsync);
                RunFuncTaskSynchronously(configurationGeneralProperties.AssemblyName.SetValueAsync, (object)value);
            }
        }

        public string FullPath
        {
            get
            {
                var configurationGeneralProperties = JoinableTaskFactory.Run(ProjectProperties.GetConfigurationGeneralPropertiesAsync);
                return JoinableTaskFactory.Run(configurationGeneralProperties.TargetPath.GetEvaluatedValueAtEndAsync);
            }
        }

        public string ExtenderCATID => null;

        public string AbsoluteProjectDirectory
        {
            get
            {
                var configurationGeneralBrowseObjectProperties = JoinableTaskFactory.Run(ProjectProperties.GetConfigurationGeneralBrowseObjectPropertiesAsync);
                return JoinableTaskFactory.Run(configurationGeneralBrowseObjectProperties.FullPath.GetEvaluatedValueAtEndAsync);
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

        public object ExtenderNames => throw new NotImplementedException();

        public string ApplicationIcon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjOptionStrict OptionStrict { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string ReferencePath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string OutputFileName => throw new NotImplementedException();

        public prjOptionExplicit OptionExplicit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjCompare OptionCompare { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjProjectType ProjectType => throw new NotImplementedException();

        public string DefaultNamespace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public object get_Extender(string ExtenderName)
        {
            throw new NotImplementedException();
        }

    }
}
