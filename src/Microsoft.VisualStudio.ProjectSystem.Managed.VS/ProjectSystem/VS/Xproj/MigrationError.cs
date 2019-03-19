// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    /// <summary>
    /// Migration report holder. Copied from
    /// https://github.com/dotnet/cli/blob/rel/1.0.0/src/Microsoft.DotNet.ProjectJsonMigration/MigrationError.cs
    /// </summary>
    internal class MigrationError
    {
        [JsonProperty]
        public string ErrorCode { get; private set; }

        [JsonProperty]
        public string GeneralErrorReason { get; private set; }

        [JsonProperty]
        public string Message { get; private set; }

        [JsonIgnore]
        // Note: this format is the same format that is used when dotnet migrate prints to the console. These messages should already be
        // as localized as the cli is, as they come from the cli.
        public string FormattedErrorMessage => $"{ErrorCode}::{GeneralErrorReason}: {Message}";

        public MigrationError() { }

        public MigrationError(string errorCode, string generalErrorReason, string message)
        {
            ErrorCode = errorCode;
            GeneralErrorReason = generalErrorReason;
            Message = message;
        }

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
            return ((ErrorCode?.GetHashCode() ?? 1 * 31) + GeneralErrorReason?.GetHashCode() ?? 1 * 31) + Message?.GetHashCode() ?? 1 * 31;
        }
    }
}
