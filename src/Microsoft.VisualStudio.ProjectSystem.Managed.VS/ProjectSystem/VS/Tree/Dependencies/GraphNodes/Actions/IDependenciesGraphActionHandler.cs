// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    /// <summary>
    /// Handles graph progression requests via <see cref="IGraphProvider.BeginGetGraphData"/>.
    /// </summary>
    /// <remarks>
    /// Implementations perform requests such as:
    /// <list type="bullet">
    ///   <item>Does a node have children?</item>
    ///   <item>What are a node's children?</item>
    ///   <item>What are search results for a node?</item>
    /// </list>
    /// </remarks>
    internal interface IDependenciesGraphActionHandler
    {
        bool CanHandleRequest(IGraphContext graphContext);
        bool HandleRequest(IGraphContext graphContext);
    }
}
