// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
