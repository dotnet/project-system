// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Exports a <see cref="ILaunchProfileExtensionValueProvider" /> or <see cref="IGlobalSettingExtensionValueProvider"/>.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class ExportLaunchProfileExtensionValueProviderAttribute : ExportAttribute
    {
        public string[] PropertyNames { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportLaunchProfileExtensionValueProviderAttribute"/>
        /// class for a single intercepted property.
        /// </summary>
        public ExportLaunchProfileExtensionValueProviderAttribute(string propertyName, ExportLaunchProfileExtensionValueProviderScope scope)
            : this(new[] { propertyName }, scope)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportLaunchProfileExtensionValueProviderAttribute"/>
        /// class for multiple intercepted properties.
        /// </summary>
        public ExportLaunchProfileExtensionValueProviderAttribute(string[] propertyNames, ExportLaunchProfileExtensionValueProviderScope scope)
            : base(GetType(scope))
        {
            PropertyNames = propertyNames;
        }

        private static Type? GetType(ExportLaunchProfileExtensionValueProviderScope scope)
        {
            return scope switch
            {
                ExportLaunchProfileExtensionValueProviderScope.LaunchProfile => typeof(ILaunchProfileExtensionValueProvider),
                ExportLaunchProfileExtensionValueProviderScope.GlobalSettings => typeof(IGlobalSettingExtensionValueProvider),

                _ => null
            };
        }
    }
}
