// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// <para>
    /// A property provider that extends <see cref="ILaunchSettings.GlobalSettings"/> to
    /// support reading and writing arbitrary properties.
    /// </para>
    /// <para>
    /// This is necessary to convert back and forth between the <see cref="string"/>
    /// representation of the property value used by the properties system and the <see cref="object"/>
    /// representation stored in the global settings. However, it can also be used for
    /// validation and transformation of the property value, or to update other global
    /// settings in response. 
    /// </para>
    /// <para>
    /// Implementations of this must be tagged with the <see cref="ExportLaunchProfileExtensionValueProviderAttribute"/>
    /// using the <see cref="ExportLaunchProfileExtensionValueProviderScope.GlobalSettings"/>
    /// scope.
    /// </para>
    /// </summary>
    /// <remarks>
    /// See also <see cref="ILaunchProfileExtensionValueProvider"/> for the equivalent
    /// interface for global launch settings, and <see cref="IInterceptingPropertyValueProvider"/>
    /// for a similar interface for intercepting callbacks for properties stored in
    /// MSBuild files.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension, Cardinality = Composition.ImportCardinality.ZeroOrMore)]
    public interface IGlobalSettingExtensionValueProvider
    {
        /// <summary>
        /// Reads the given property from the <paramref name="globalSettings"/>, converting
        /// it to a <see cref="string"/>.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property, as known to the CPS properties system.
        /// </param>
        /// <param name="globalSettings">
        /// The current set of global launch settings.
        /// </param>
        /// <param name="rule">
        /// An optional <see cref="Rule"/> associated with the <see cref="IProjectProperties"/>
        /// calling this provider.
        /// </param>
        /// <returns>
        /// The value of the given property, as a string.
        /// </returns>
        /// <remarks>
        /// The <paramref name="rule"/> provides access to metadata that may influence the
        /// conversion of the property to a <see cref="string"/>.
        /// </remarks>
        string OnGetPropertyValue(string propertyName, ImmutableDictionary<string, object> globalSettings, Rule? rule);

        /// <summary>
        /// Converts the <paramref name="propertyValue"/> from a <see cref="string"/> and
        /// produces a set of changes to apply to the <paramref name="globalSettings"/>.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property, as known to the CPS properties system.
        /// </param>
        /// <param name="propertyValue">
        /// The new value of the property.
        /// </param>
        /// <param name="globalSettings">
        /// The current set of global launch settings.
        /// </param>
        /// <param name="rule">
        /// An optional <see cref="Rule"/> associated with the <see cref="IProjectProperties"/>
        /// calling this provider.</param>
        /// <returns>
        /// A set of changes to apply to the global settings. If the value for a given key
        /// is <see langword="null" /> the corresponding global setting will be removed,
        /// otherwise the setting will be updated.
        /// </returns>
        /// <remarks>
        /// The <paramref name="rule"/> provides access to metadata that may influence the
        /// conversion of the property from a <see cref="string"/>.
        /// </remarks>
        ImmutableDictionary<string, object?> OnSetPropertyValue(string propertyName, string propertyValue, ImmutableDictionary<string, object> globalSettings, Rule? rule);
    }
}
