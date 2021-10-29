// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Runtimes
{
    internal readonly struct RuntimeDescriptor
    {
        internal static readonly RuntimeDescriptor Empty = new(string.Empty);

        public RuntimeDescriptor(string sdkRuntime)
        {
            SdkRuntime = sdkRuntime;
        }

        public string SdkRuntime { get; }
    }
}
