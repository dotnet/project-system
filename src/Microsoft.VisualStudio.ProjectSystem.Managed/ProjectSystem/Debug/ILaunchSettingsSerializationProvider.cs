// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Interface implemented by providers of custom data. When the launch settings file is read the top level token matching the attribute
    /// "JsonSection" is invoked to deserialize the json to an object. The export needs the attribute
    /// [ExportMetadata("JsonSection", "nameofjsonsection")]
    /// [ExportMetadata("SerializationProvider", typeof(objectToSerialize))]
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    public interface ILaunchSettingsSerializationProvider
    {
    }
}
