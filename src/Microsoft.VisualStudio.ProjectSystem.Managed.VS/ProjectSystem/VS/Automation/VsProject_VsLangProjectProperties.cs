// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using VSLangProj;
using VSLangProj110;
using VSLangProj165;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    public partial class VSProject : VSLangProj.ProjectProperties
    {
        private T GetBrowseObjectValue<T>(Func<ConfigurationGeneralBrowseObject, IEvaluatedProperty> func)
        {
            return _threadingService.ExecuteSynchronously(async () =>
            {
                ConfigurationGeneralBrowseObject browseObject = await _projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync();
                IEvaluatedProperty evaluatedProperty = func(browseObject);
                object? value = await evaluatedProperty.GetValueAsync();

                // IEvaluatedProperty.GetValueAsync always returns a non-null value, though
                // it inherits this method from IProperty which can return null.
                return (T)value!;
            });
        }

        private void SetBrowseObjectValue(Func<ConfigurationGeneralBrowseObject, IEvaluatedProperty> func, object value)
        {
            _threadingService.ExecuteSynchronously(async () =>
            {
                ConfigurationGeneralBrowseObject browseObject = await _projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync();
                IEvaluatedProperty evaluatedProperty = func(browseObject);
                await evaluatedProperty.SetValueAsync(value);
            });
        }

        private string GetBrowseObjectValueAtEnd(Func<ConfigurationGeneralBrowseObject, IEvaluatedProperty> func)
        {
            return _threadingService.ExecuteSynchronously(async () =>
            {
                ConfigurationGeneralBrowseObject browseObject = await _projectProperties.Value.GetConfigurationGeneralBrowseObjectPropertiesAsync();
                IEvaluatedProperty evaluatedProperty = func(browseObject);
                return await evaluatedProperty.GetEvaluatedValueAtEndAsync();
            });
        }

        private string GetValueAtEnd(Func<ConfigurationGeneral, IEvaluatedProperty> func)
        {
            return _threadingService.ExecuteSynchronously(async () =>
            {
                ConfigurationGeneral configurationGeneral = await _projectProperties.Value.GetConfigurationGeneralPropertiesAsync();
                IEvaluatedProperty evaluatedProperty = func(configurationGeneral);
                return await evaluatedProperty.GetEvaluatedValueAtEndAsync();
            });
        }

        private void SetValue(Func<ConfigurationGeneral, IEvaluatedProperty> func, object value)
        {
            _threadingService.ExecuteSynchronously(async () =>
            {
                ConfigurationGeneral configurationGeneral = await _projectProperties.Value.GetConfigurationGeneralPropertiesAsync();
                IEvaluatedProperty evaluatedProperty = func(configurationGeneral);
                await evaluatedProperty.SetValueAsync(value);
            });
        }

        public prjOutputTypeEx OutputTypeEx
        {
            get => GetBrowseObjectValue<prjOutputTypeEx>(browseObject => browseObject.OutputType);
            set => SetBrowseObjectValue(browseObject => browseObject.OutputType, value);
        }

        // Implementation of VsLangProj.ProjectProperties
        public prjOutputType OutputType
        {
            get => GetBrowseObjectValue<prjOutputType>(browseObject => browseObject.OutputType);
            set => SetBrowseObjectValue(browseObject => browseObject.OutputType, value);
        }

        public string AssemblyName
        {
            get => GetValueAtEnd(configurationGeneral => configurationGeneral.AssemblyName);
            set => SetValue(configurationGeneral => configurationGeneral.AssemblyName, value);
        }

        public string FullPath
        {
            get => GetValueAtEnd(configurationGeneral => configurationGeneral.ProjectDir);
        }

        public string OutputFileName
        {
            get => GetBrowseObjectValueAtEnd(browseObject => browseObject.OutputFileName);
        }

        public string? ExtenderCATID => null;

        public string AbsoluteProjectDirectory
        {
            get => GetBrowseObjectValueAtEnd(browseObject => browseObject.FullPath);
        }

        public bool AutoGenerateBindingRedirects
        {
            get => GetBrowseObjectValue<bool?>(browseObject => browseObject.AutoGenerateBindingRedirects).GetValueOrDefault();
            set => SetBrowseObjectValue(browseObject => browseObject.AutoGenerateBindingRedirects, value);
        }

        public AuthenticationMode AuthenticationMode
        {
            get => GetBrowseObjectValue<AuthenticationMode?>(browseObject => browseObject.AuthenticationMode).GetValueOrDefault();
            set => SetBrowseObjectValue(browseObject => browseObject.AuthenticationMode, value);
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

        public prjOptionExplicit OptionExplicit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjCompare OptionCompare { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public prjProjectType ProjectType => throw new NotImplementedException();

        public string DefaultNamespace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public object get_Extender(string ExtenderName) => throw new NotImplementedException();

        object VSLangProj.ProjectProperties.Extender => throw new NotImplementedException();
    }
}
