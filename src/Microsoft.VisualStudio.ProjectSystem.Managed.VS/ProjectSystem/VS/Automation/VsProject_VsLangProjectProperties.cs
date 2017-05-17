// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using VSLangProj;
using VSLangProj110;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    public partial class VSProject : VSLangProj.ProjectProperties
    {
        private readonly Dictionary<string, int> _getOutputTypeExMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            {"WinExe", 0 },
            {"Exe", 1 },
            {"Library", 2 },
            {"AppContainerExe", 3 },
            {"WinMDObj", 4 },
        };

        private readonly Dictionary<int, string> _setOutputTypeExMap = new Dictionary<int, string>
        {
            {0, "WinExe"},
            {1, "Exe"},
            {2, "Library"},
            {3, "AppContainerExe"},
            {4, "WinMDObj"},
        };

        /// Provides the value for <see cref="OutputType"/> of enum type <see cref="prjOutputType"/> whose value is only 0, 1 or 2.
        private readonly Dictionary<string, int> _getOutputTypeMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            {"WinExe", 0 },
            {"Exe", 1 },
            {"Library", 2 },
            {"AppContainerExe", 1 },
            {"WinMDObj", 2 },
        };

        private ProjectProperties ProjectProperties
        {
            get { return _projectProperties.Value; }
        }

        public prjOutputTypeEx OutputTypeEx
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    var configurationGeneralProperties = await ProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                    var value = await configurationGeneralProperties.OutputType.GetEvaluatedValueAtEndAsync().ConfigureAwait(true);
                    return (prjOutputTypeEx)_getOutputTypeExMap[value];
                });
            }

            set
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    var OutputTypeValue = _setOutputTypeExMap[(int)value];
                    var configurationGeneralProperties = await ProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                    await configurationGeneralProperties.OutputType.SetValueAsync(OutputTypeValue).ConfigureAwait(true);
                });
            }
        }


        // Implementation of VsLangProj.ProjectProperties
        public prjOutputType OutputType
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    var configurationGeneralProperties = await ProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                    var value = await configurationGeneralProperties.OutputType.GetEvaluatedValueAtEndAsync().ConfigureAwait(true);
                    return (prjOutputType)_getOutputTypeMap[value];
                });
            }

            set
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    var OutputTypeValue = _setOutputTypeExMap[(int)value];
                    var configurationGeneralProperties = await ProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                    await configurationGeneralProperties.OutputType.SetValueAsync(OutputTypeValue).ConfigureAwait(true);
                });
            }
        }

        public string AssemblyName
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    var configurationGeneralProperties = await ProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                    return await configurationGeneralProperties.AssemblyName.GetEvaluatedValueAtEndAsync().ConfigureAwait(true);
                });
            }

            set
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    var browseObjectProperties = await ProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                    await browseObjectProperties.AssemblyName.SetValueAsync(value).ConfigureAwait(true);
                });
            }
        }

        public string FullPath
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    var configurationGeneralProperties = await ProjectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                    return await configurationGeneralProperties.TargetPath.GetEvaluatedValueAtEndAsync().ConfigureAwait(true);
                });
            }
        }

        public string ExtenderCATID => null;

        public string AbsoluteProjectDirectory
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    var browseObjectProperties = await ProjectProperties.GetConfigurationGeneralBrowseObjectPropertiesAsync().ConfigureAwait(true);
                    return await browseObjectProperties.FullPath.GetEvaluatedValueAtEndAsync().ConfigureAwait(true);
                });
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
