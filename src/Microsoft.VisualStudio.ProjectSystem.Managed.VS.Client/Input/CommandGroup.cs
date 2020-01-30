// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
