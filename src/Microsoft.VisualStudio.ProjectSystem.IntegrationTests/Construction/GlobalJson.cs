// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Defines a <c>global.json</c> file to be created when using <see cref="ProjectLayoutTestBase"/>.
    /// </summary>
    public sealed class GlobalJson
    {
        public string SdkVersion { get; }

        public GlobalJson(string sdkVersion) => SdkVersion = sdkVersion;

        public void Save(string rootPath)
        {
            File.WriteAllText(
                Path.Combine(rootPath, "global.json"),
                $"{{ \"sdk\": {{ \"version\": \"{SdkVersion}\" }} }}");
        }
    }
}
