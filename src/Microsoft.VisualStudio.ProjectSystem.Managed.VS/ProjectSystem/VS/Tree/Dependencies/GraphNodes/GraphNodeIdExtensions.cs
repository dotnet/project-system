// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal static class GraphNodeIdExtensions
    {
        internal static string? GetValue(this GraphNodeId id, GraphNodeIdName idPartName)
        {
            if (idPartName == CodeGraphNodeIdName.Assembly || idPartName == CodeGraphNodeIdName.File)
            {
                try
                {
                    Uri value = id.GetNestedValueByName<Uri>(idPartName);

                    // for idPartName == CodeGraphNodeIdName.File it can be null, avoid unnecessary exception
                    if (value == null)
                    {
                        return null;
                    }

                    // Assembly and File are represented by a Uri, extract LocalPath string from Uri
                    return (value.IsAbsoluteUri ? value.LocalPath : value.ToString()).Trim('/');
                }
                catch
                {
                    // for some node ids Uri might throw format exception, thus try to get string at least
                    return id.GetNestedValueByName<string>(idPartName);
                }
            }
            else
            {
                return id.GetNestedValueByName<string>(idPartName);
            }
        }
    }
}
