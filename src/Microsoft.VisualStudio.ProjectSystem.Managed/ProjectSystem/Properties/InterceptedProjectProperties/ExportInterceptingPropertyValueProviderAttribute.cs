// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Exports a <see cref="IInterceptingPropertyValueProvider"/> extension to CPS.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    internal sealed class ExportInterceptingPropertyValueProviderAttribute : ExportAttribute
    {
        public string[] PropertyNames { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportInterceptingPropertyValueProviderAttribute"/> class.
        /// </summary>
        public ExportInterceptingPropertyValueProviderAttribute(string propertyName, ExportInterceptingPropertyValueProviderFile file)
            : this(new[] { propertyName }, file)
        {
        }

        public ExportInterceptingPropertyValueProviderAttribute(string[] propertyNames, ExportInterceptingPropertyValueProviderFile file)
            : base(GetFile(file), typeof(IInterceptingPropertyValueProvider))
        {
            PropertyNames = propertyNames;
        }

        private static string GetFile(ExportInterceptingPropertyValueProviderFile file)
        {
            return file switch
            {
                ExportInterceptingPropertyValueProviderFile.ProjectFile => ContractNames.ProjectPropertyProviders.ProjectFile,
                ExportInterceptingPropertyValueProviderFile.UserFile => ContractNames.ProjectPropertyProviders.UserFile,
                ExportInterceptingPropertyValueProviderFile.UserFileWithXamlDefaults => ContractNames.ProjectPropertyProviders.UserFileWithXamlDefaults,

                _ => string.Empty,
            };
        }
    }

    internal enum ExportInterceptingPropertyValueProviderFile
    {
        ProjectFile,
        UserFile,
        UserFileWithXamlDefaults
    }
}
