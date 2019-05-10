// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides a method for retrieving options from the host environment.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IEnvironmentOptions
    {
        /// <summary>
        ///     Returns the value associated with the specified category, page and option, if it exists, 
        ///     otherwise, returns <paramref name="defaultValue"/>.
        /// </summary>
        /// <param name="category">
        ///     A <see cref="string"/> containing the category of the option to return.
        /// </param>
        /// <param name="page">
        ///     A <see cref="string"/> containing the page of the option to return.
        /// </param>
        /// <param name="option">
        ///     A <see cref="string"/> containing the name of the option to return.
        /// </param>
        /// <param name="defaultValue">
        ///     The value to return if the value does not exist.
        /// </param>
        T GetOption<T>(string category, string page, string option, T defaultValue);
    }
}
