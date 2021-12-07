// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;
using VerifyXunit;
using Xunit;

namespace Microsoft.VisualStudio.Setup
{
    [UsesVerify]
    public sealed class PackageContentTests
    {
        [Fact]
        public Task ProjectSystem()
        {
            IEnumerable<string> files = GetPackageContents("ProjectSystem.vsix");

            return Verifier.Verify(files);
        }

        [Fact]
        public Task VisualStudioEditorsSetup()
        {
            IEnumerable<string> files = GetPackageContents("VisualStudioEditorsSetup.vsix");

            return Verifier.Verify(files);
        }

        [Fact]
        public Task CommonFiles()
        {
            IEnumerable<string> files = GetPackageContents("Microsoft.VisualStudio.ProjectSystem.Managed.CommonFiles.vsix");

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

            // D:\repos\project-system\artifacts\Debug\VSSetup\ProjectSystem.vsix

            var vsixPath = Path.Combine(
                rootPath,
                "artifacts",
                config,
                "VSSetup",
                "Insertion",
                vsixName);
            
            using var archive = ZipFile.OpenRead(vsixPath);
            
            return archive.Entries.Select(entry => entry.FullName);
        }
    }
}
