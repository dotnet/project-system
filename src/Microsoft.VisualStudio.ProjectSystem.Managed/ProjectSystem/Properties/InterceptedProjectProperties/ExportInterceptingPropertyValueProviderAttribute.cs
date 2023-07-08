// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Exports a <see cref="IInterceptingPropertyValueProvider"/> extension to CPS.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class ExportInterceptingPropertyValueProviderAttribute : ExportAttribute
    {
        public string[] PropertyNames { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportInterceptingPropertyValueProviderAttribute"/>
        /// class for a single intercepted property.
        /// </summary>
        public ExportInterceptingPropertyValueProviderAttribute(string propertyName, ExportInterceptingPropertyValueProviderFile file)
            : this(new[] { propertyName }, file)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportInterceptingPropertyValueProviderAttribute"/>
        /// class for multiple intercepted properties.
        /// </summary>
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

    /// <summary>
    /// Specifies the "backing store" for an <see cref="IInterceptingPropertyValueProvider"/>.
    /// This determines where the original (non-intercepted) value is retrieved from/stored to.
    /// </summary>
    public enum ExportInterceptingPropertyValueProviderFile
    {
        /// <summary>
        /// Intercepted properties are backed by the property provider that reads/writes
        /// from the project file.
        /// </summary>
        ProjectFile,
        /// <summary>
        /// Intercepted properties are backed by the property provider that reads/writes
        /// from the user file.
        /// </summary>
        UserFile,
        /// <summary>
        /// Intercepted properties are backed by the property provider that reads/writes
        /// from the user file, except that default values come from the underlying XAML
        /// file instead of elsewhere in the project (e.g., an imported .props file).
        /// </summary>
        UserFileWithXamlDefaults
    }
}
