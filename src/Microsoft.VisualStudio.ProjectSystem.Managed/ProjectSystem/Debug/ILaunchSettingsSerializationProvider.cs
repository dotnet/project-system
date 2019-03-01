// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
