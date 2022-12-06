// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        /// <summary>
        /// Potential values for the <c>CopyToOutputDirectory</c> metadata on items.
        /// </summary>
        internal enum CopyType
        {
            PreserveNewest,
            Always
        }
    }
}
