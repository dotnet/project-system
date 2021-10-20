// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Provides for the temporary storage of property values.
    /// Consider the project property pages. In some cases, changing the value of property A
    /// will cause us to clear the value of property B. If A is changed back to its original
    /// value, we would like to be able to restore the previous value of B as well. This
    /// type provides the means to store that previous value until (and if) we need it again.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ITemporaryPropertyStorage
    {
        void AddOrUpdatePropertyValue(string propertyName, string propertyValue);
        string? GetPropertyValue(string propertyName);
    }
}
