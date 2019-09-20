// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Host, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ITargetFrameworkProvider
    {
        /// <summary>
        /// Parses full tfm or short framework name and returns a corresponding <see cref="ITargetFramework"/>
        /// instance, or <see langword="null" /> if the framework name has an invalid format.
        /// </summary>
        ITargetFramework? GetTargetFramework(string? shortOrFullName);

        /// <summary>
        /// Returns the item in <paramref name="otherFrameworks"/> that is most compatible/closest to
        /// <paramref name="targetFramework"/>, or <see langword="null" /> if none are compatible.
        /// </summary>
        ITargetFramework? GetNearestFramework(ITargetFramework? targetFramework, IEnumerable<ITargetFramework>? otherFrameworks);
    }
}
