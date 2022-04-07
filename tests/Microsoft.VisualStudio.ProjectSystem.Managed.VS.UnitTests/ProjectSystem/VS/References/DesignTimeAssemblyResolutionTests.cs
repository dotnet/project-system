// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;
using VSLangProj110;
using VSLangProj80;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    public class DesignTimeAssemblyResolutionTests
    {
        [Fact]
        public void GetTargetFramework_WhenUnderlyingGetPropertyReturnsHResult_ReturnsHResult()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(VSConstants.E_INVALIDARG);
            var resolution = CreateInstance(hierarchy);

            var result = resolution.GetTargetFramework(out string _);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
        }

        [Fact]
        public void GetTargetFramework_WhenUnderlyingGetPropertyReturnsHResult_SetsTargetFrameworkToNull()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(VSConstants.E_INVALIDARG);
            var resolution = CreateInstance(hierarchy);

            resolution.GetTargetFramework(out string? result);

            Assert.Null(result);
        }

        [Fact]
        public void GetTargetFramework_WhenUnderlyingGetPropertyReturnsDISP_E_MEMBERNOTFOUND_ReturnsOK()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(VSConstants.DISP_E_MEMBERNOTFOUND);
            var resolution = CreateInstance(hierarchy);

            var result = resolution.GetTargetFramework(out string _);

            Assert.Equal(VSConstants.S_OK, result);
        }

        [Fact]
        public void GetTargetFramework_WhenUnderlyingGetPropertyReturnsDISP_E_MEMBERNOTFOUND_SetsTargetFrameworkToNull()
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(VSConstants.DISP_E_MEMBERNOTFOUND);
            var resolution = CreateInstance(hierarchy);

            resolution.GetTargetFramework(out string? result);

            Assert.Null(result);
        }

        [Fact]
        public void GetTargetFramework_WhenDisposed_ReturnUnexpected()
        {
            var resolution = CreateInstance();
            resolution.Dispose();

            var result = resolution.GetTargetFramework(out _);

            Assert.Equal(VSConstants.E_UNEXPECTED, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(".NETFramework, Version=v4.5")]
        [InlineData(".NETFramework, Version=v4.5, Profile=Client")]
        public void GetTargetFramework_WhenUnderlyingGetPropertyReturnsValue_SetsTargetFramework(string input)
        {
            var hierarchy = IVsHierarchyFactory.ImplementGetProperty(input);
            var resolution = CreateInstance(hierarchy);

            var hr = resolution.GetTargetFramework(out string? result);

            Assert.Equal(input, result);
            Assert.Equal(VSConstants.S_OK, hr);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_NullAsAssemblySpecs_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(null, 1, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_ZeroAsAssembliesToResolve_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 0, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_NullAsResolvedAssemblyPaths_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 1, null, out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_MoreAssemblySpecsThanAssembliesToResolve_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib", "System" }, 1, new VsResolvedAssemblyPath[2], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_MoreAssemblySpecsThanResolvedAssemblyPaths_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib", "System" }, 2, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_MoreAssembliesToResolveThanAssemblySpecs_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 2, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_MoreResolvedAssemblyPathsThanAssemblySpecs_ReturnsE_INVALIDARG()
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 1, new VsResolvedAssemblyPath[2], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_UnresolvedAssembly_SetsResolvedAssemblyPathsToZero()
        {   // BUG: https://devdiv.visualstudio.com/DevDiv/_workitems?id=368836
            var reference = Reference3Factory.CreateAssemblyReference("mscorlib", "1.0.0.0");

            var resolution = CreateInstance(reference);

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 1, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.S_OK, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_ComReference_SetsResolvedAssemblyPathsToZero()
        {
            var reference = Reference3Factory.CreateAssemblyReference("mscorlib", "1.0.0.0", type: prjReferenceType.prjReferenceTypeActiveX, refType: __PROJECTREFERENCETYPE.PROJREFTYPE_ACTIVEX);

            var resolution = CreateInstance(reference);

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 1, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.S_OK, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_SdkReference_SetsResolvedAssemblyPathsToZero()
        {
            // SDKs say they are "assemblies" for Reference.Type, but SDK for Reference.RefType
            var reference = Reference3Factory.CreateAssemblyReference("mscorlib", "1.0.0.0", type: prjReferenceType.prjReferenceTypeAssembly, refType: (__PROJECTREFERENCETYPE)__PROJECTREFERENCETYPE2.PROJREFTYPE_SDK);

            var resolution = CreateInstance(reference);

            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { "mscorlib" }, 1, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.S_OK, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("System, Bar")]
        [InlineData("System, Version=NotAVersion")]
        [InlineData("System, PublicKeyToken=ABC")]
        public void ResolveAssemblyPathInTargetFx_InvalidNameAsAssemblySpec_ReturnsE_INVALIDARG(string? input)
        {
            var resolution = CreateInstance();

            var result = resolution.ResolveAssemblyPathInTargetFx(new string?[] { input }, 1, new VsResolvedAssemblyPath[1], out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.E_INVALIDARG, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
        }

        [Theory]    // Input                                                                        // Name             // Version          // Path
        [InlineData("System",                                                                       "System",           "",                 @"C:\System.dll")]
        [InlineData("System",                                                                       "System",           "1",                @"C:\System.dll")]
        [InlineData("System",                                                                       "System",           "1.0",              @"C:\System.dll")]
        [InlineData("System",                                                                       "System",           "1.0.0",            @"C:\System.dll")]
        [InlineData("System",                                                                       "System",           "1.0.0.0",          @"C:\System.dll")]
        [InlineData("System.Foo",                                                                   "System.Foo",       "1.0.0.0",          @"C:\System.Foo.dll")]
        [InlineData("System, Version=1.0.0.0",                                                      "System",           "1.0.0.0",          @"C:\System.dll")]
        [InlineData("System, Version=1.0.0.0",                                                      "System",           "2.0.0.0",          @"C:\System.dll")]      // We let a later version satisfy an earlier version
        [InlineData("System, Version=1.0",                                                          "System",           "2.0.0.0",          @"C:\System.dll")]
        [InlineData("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",    "System",           "4.0.0.0",          @"C:\System.dll")]
        public void ResolveAssemblyPathInTargetFx_NameThatMatches_ReturnsResolvedPaths(string input, string name, string version, string path)
        {
            var reference = Reference3Factory.CreateAssemblyReference(name, version, path);

            var resolution = CreateInstance(reference);

            var resolvedPaths = new VsResolvedAssemblyPath[1];
            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { input }, 1, resolvedPaths, out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.S_OK, result);
            Assert.Equal(1u, resolvedAssemblyPaths);
            Assert.Equal(input, resolvedPaths[0].bstrOrigAssemblySpec);
            Assert.Equal(path, resolvedPaths[0].bstrResolvedAssemblyPath);
        }

        [Theory]    // Input                                                                        // Name             // Version          // Path
        [InlineData("System",                                                                       "System.Core",      "",                 @"C:\System.Core.dll")]
        [InlineData("System",                                                                       "system",           "",                 @"C:\System.dll")]
        [InlineData("encyclopædia",                                                                 "encyclopaedia",    "",                 @"C:\System.dll")]
        [InlineData("System, Version=1.0.0.0",                                                      "System",           "",                 @"C:\System.dll")]
        [InlineData("System, Version=2.0.0.0",                                                      "System",           "1.0.0.0",          @"C:\System.dll")]
        public void ResolveAssemblyPathInTargetFx_NameThatDoesNotMatch_SetsResolvedAssemblyPathsToZero(string input, string name, string version, string path)
        {
            var reference = Reference3Factory.CreateAssemblyReference(name, version, path);

            var resolution = CreateInstance(reference);

            var resolvedPaths = new VsResolvedAssemblyPath[1];
            var result = resolution.ResolveAssemblyPathInTargetFx(new string[] { input }, 1, resolvedPaths, out uint resolvedAssemblyPaths);

            Assert.Equal(VSConstants.S_OK, result);
            Assert.Equal(0u, resolvedAssemblyPaths);
            Assert.Null(resolvedPaths[0].bstrOrigAssemblySpec);
            Assert.Null(resolvedPaths[0].bstrResolvedAssemblyPath);
        }

        [Fact]
        public void ResolveAssemblyPathInTargetFx_WhenDisposed_ReturnUnexpected()
        {
            var resolution = CreateInstance();
            resolution.Dispose();

            var result = resolution.ResolveAssemblyPathInTargetFx(new[] { "System" }, 1, new VsResolvedAssemblyPath[1], out _);

            Assert.Equal(VSConstants.E_UNEXPECTED, result);
        }

        private static DesignTimeAssemblyResolution CreateInstance(params Reference[] references)
        {
            VSProject vsProject = VSProjectFactory.ImplementReferences(references);
            Project project = ProjectFactory.ImplementObject(() => vsProject);
            IVsHierarchy hierarchy = IVsHierarchyFactory.ImplementGetProperty(project);

            return CreateInstance(hierarchy);
        }

        private static DesignTimeAssemblyResolution CreateInstance(IVsHierarchy? hierarchy = null)
        {
            hierarchy ??= IVsHierarchyFactory.Create();

            IUnconfiguredProjectVsServices projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy);

            return new DesignTimeAssemblyResolution(projectVsServices);
        }
    }
}
