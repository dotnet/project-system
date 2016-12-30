// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    /// <summary>
    /// Migration report holder. Copied from
    /// https://github.com/dotnet/cli/blob/rel/1.0.0/src/Microsoft.DotNet.ProjectJsonMigration/MigrationError.cs
    /// </summary>
    internal class MigrationError
    {
        public string ErrorCode { get; set; }

        public string GeneralErrorReason { get; set; }

        public string Message { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is MigrationError otherError)
            {
                return Equals(otherError.ErrorCode, ErrorCode) &&
                    Equals(otherError.GeneralErrorReason, GeneralErrorReason) &&
                    Equals(otherError.Message, Message);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ((ErrorCode?.GetHashCode() ?? 1 * 409) + GeneralErrorReason?.GetHashCode() ?? 1 * 409) + Message?.GetHashCode() ?? 1;
        }
    }
}
