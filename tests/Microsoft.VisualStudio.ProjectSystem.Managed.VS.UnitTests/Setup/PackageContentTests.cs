// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO.Compression;
using Microsoft.VisualStudio.Utilities;
using VerifyTests;
using VerifyXunit;

namespace Microsoft.VisualStudio.Setup
{
    [UsesVerify]
    public sealed class PackageContentTests
    {
        // These files are only added as part of signing.
        private const string DigitalSignature = "package/services/digital-signature";
        private const string Rels = "_rels/.rels";

        [Fact]
        public Task ProjectSystem()
        {
            IEnumerable<string> files = GetPackageContents("ProjectSystem.vsix");
            VerifierSettings.ScrubLinesContaining(DigitalSignature, Rels);
            return Verifier.Verify(files);
        }

        [Fact]
        public Task VisualStudioEditorsSetup()
        {
            IEnumerable<string> files = GetPackageContents("VisualStudioEditorsSetup.vsix");
            VerifierSettings.ScrubLinesContaining(DigitalSignature, Rels);
            return Verifier.Verify(files);
        }

        [Fact]
        public Task CommonFiles()
        {
            IEnumerable<string> files = GetPackageContents("Microsoft.VisualStudio.ProjectSystem.Managed.CommonFiles.vsix");
            VerifierSettings.ScrubLinesContaining(DigitalSignature);
            // manifest.json is the last line for non-signed builds.
            // It will not contain a comma in this situation, so we need special logic for that.
            VerifierSettings.ScrubLinesWithReplace(s => s.EndsWith("manifest.json") ? "  manifest.json," : s);
            return Verifier.Verify(files);
        }

        private static IEnumerable<string> GetPackageContents(string vsixName)
        {
            var rootPath = RepoUtil.FindRepoRootPath();

#if DEBUG
            var config = "Debug";
#elif RELEASE
            var config = "Release";
#else
#error Unexpected configuration
#endif

            // D:\repos\project-system\artifacts\Debug\VSSetup\Insertion\ProjectSystem.vsix

            var vsixPath = Path.Combine(
                rootPath,
                "artifacts",
                config,
                "VSSetup",
                "Insertion",
                vsixName);

            using var archive = ZipFile.OpenRead(vsixPath);

            return archive.Entries.Select(entry => entry.FullName).OrderBy(fn => fn);
        }
    }
}
