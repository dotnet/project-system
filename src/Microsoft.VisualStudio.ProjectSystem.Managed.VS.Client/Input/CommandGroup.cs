// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.Input
{
    /// <summary>
    /// Specifies the command groups handled in this project.
    /// </summary>
    internal static class CommandGroup
    {
        public const string ManagedProjectSystemClientProjectCommandSet = "{28C12D02-11CB-437D-B84D-9CEA7A5333A3}";

        public static readonly Guid ManagedProjectSystemClientProjectCommandSetGuid = Guid.Parse(ManagedProjectSystemClientProjectCommandSet);
    }
}
