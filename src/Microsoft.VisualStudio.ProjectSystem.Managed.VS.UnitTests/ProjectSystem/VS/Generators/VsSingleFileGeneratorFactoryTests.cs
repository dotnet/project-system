using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [Trait("UnitTest", "ProjectSystem")]
    public class VsSingleFileGeneratorFactoryTests
    {
        public static Guid PackageGuid = Guid.Parse("860A27C0-B665-47F3-BC12-637E16A1050A");

        private static Guid ResXGuid = Guid.Parse(SingleFileGenerators.ResXGuid);

        [Theory]
        [InlineData(1, 0, 1)]
        [InlineData(0, 1, 0)]
        [InlineData(2, 3, 4)]
        public void GetGeneratorInformation_GivenValidRegistry_RetrievesData(int designTimeSource, int sharedDesignTimeSource, int compileFlag)
        {
            var manager = CreateManager(true, designTimeSource, sharedDesignTimeSource, compileFlag);
            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var factory = CreateInstance(manager, integrationService);

            Assert.Equal(VSConstants.S_OK,
                factory.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));

            Assert.Equal(designTimeSource == 1 ? 1 : 0, actualDesignTime);
            Assert.Equal(sharedDesignTimeSource == 1 ? 1 : 0, actualSharedDesignTime);
            Assert.Equal(compileFlag == 1 ? 1 : 0, actualCompileFlag);
            Assert.Equal(ResXGuid, actualGuid);
        }

        [Fact]
        public void GetGeneratorInformation_GivenValidRegistry_OptionalParamsAreOptional()
        {
            var manager = CreateManager();
            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var factory = CreateInstance(manager, integrationService);

            Assert.Equal(VSConstants.S_OK,
                factory.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));

            Assert.Equal(0, actualDesignTime);
            Assert.Equal(0, actualSharedDesignTime);
            Assert.Equal(0, actualCompileFlag);
            Assert.Equal(ResXGuid, actualGuid);
        }

        [Fact]
        public void GetGeneratorInformation_NoClsid_ReturnsFail()
        {
            var manager = IVsSettingsManagerFactory.Create($"Generators\\{PackageGuid.ToString("B").ToUpper()}\\ResXCodeFileGenerator");
            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var factory = CreateInstance(manager, integrationService);

            Assert.Equal(VSConstants.E_FAIL,
                factory.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));
        }

        [Fact]
        public void GetGeneratorInformation_NoGeneratorId_ReturnsFail()
        {
            var manager = IVsSettingsManagerFactory.Create($"Generators\\{PackageGuid.ToString("B").ToUpper()}");
            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var factory = CreateInstance(manager, integrationService);

            Assert.Equal(VSConstants.E_FAIL,
                factory.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));
        }

        [Fact]
        public void GetGeneratorInformation_NoPackage_ReturnsFail()
        {
            var manager = IVsSettingsManagerFactory.Create($"Generators");
            
            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var factory = CreateInstance(manager, integrationService);

            Assert.Equal(VSConstants.E_FAIL,
                factory.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));
        }

        [Fact]
        public void GetGeneratorInformation_NoGenerators_ReturnsFail()
        {
            var manager = IVsSettingsManagerFactory.Create("");

            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var factory = CreateInstance(manager, integrationService);

            Assert.Equal(VSConstants.E_FAIL,
                factory.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));
        }

        [Fact]
        public void GetGeneratorInformation_WhenDisposed_ReturnsUnexpected()
        {
            var factory = CreateInstance();
            factory.Dispose();

            var result = factory.GetGeneratorInformation("Foo", out _, out _, out _, out _);

            Assert.Equal(VSConstants.E_UNEXPECTED, result);
        }

        private IVsSettingsManager CreateManager(bool useProperties = false, int designTimeSource = 0, int sharedDesignTimeSource = 0, int compileFlag = 0)
        {
            var vals = new Dictionary<string, object>
            {
                { "CLSID", ResXGuid.ToString() }
            };
            if (useProperties)
            {
                vals["GeneratesDesignTimeSource"] = designTimeSource;
                vals["GeneratesSharedDesignTimeSource"] = sharedDesignTimeSource;
                vals["UseDesignTimeCompilationFlag"] = compileFlag;
            }
            return IVsSettingsManagerFactory.Create($"Generators\\{PackageGuid.ToString("B").ToUpper()}\\ResXCodeFileGenerator", vals);
        }

        private static VsSingleFileGeneratorFactory CreateInstance()
        {
            return CreateInstance(IVsSettingsManagerFactory.Create(), IVsUnconfiguredProjectIntegrationServiceFactory.Create());
        }

        private static VsSingleFileGeneratorFactory CreateInstance(IVsSettingsManager settingsManager, IVsUnconfiguredProjectIntegrationService vsUnconfiguredProjectIntegrationService)
        {
            settingsManager  = settingsManager ?? IVsSettingsManagerFactory.Create();
            vsUnconfiguredProjectIntegrationService = vsUnconfiguredProjectIntegrationService ?? IVsUnconfiguredProjectIntegrationServiceFactory.Create();

            var vsService = IVsServiceFactory.Create<SVsSettingsManager, IVsSettingsManager>(settingsManager);

            return new VsSingleFileGeneratorFactory(vsService, vsUnconfiguredProjectIntegrationService);
        }
    }
}
