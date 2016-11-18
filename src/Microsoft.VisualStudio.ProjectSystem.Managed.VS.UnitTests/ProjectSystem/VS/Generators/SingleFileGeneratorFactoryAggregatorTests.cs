using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Xunit;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [ProjectSystemTrait]
    public class SingleFileGeneratorFactoryAggregatorTests
    {
        public static Guid PackageGuid = Guid.Parse("860A27C0-B665-47F3-BC12-637E16A1050A");

        private static Guid ResXGuid = Guid.Parse(SingleFileGenerators.ResXGuid);

        [Theory]
        [InlineData(1, 0, 1)]
        [InlineData(0, 1, 0)]
        [InlineData(2, 3, 4)]
        public void SingleFileGeneratorFactoryAggregator_GivenValidRegistry_RetrievesData(int designTimeSource, int sharedDesignTimeSource, int compileFlag)
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .CreateSubkey("Generators", false)
                .CreateSubkey(PackageGuid.ToString("B").ToUpper(), false)
                .CreateSubkey("ResXCodeFileGenerator", false)
                .SetValue("CLSID", ResXGuid.ToString())
                .SetValue("GeneratesDesignTimeSource", designTimeSource)
                .SetValue("GeneratesSharedDesignTimeSource", sharedDesignTimeSource)
                .SetValue("UseDesignTimeCompilationFlag", compileFlag)
                .Build();

            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService, registryHelper);

            int actualDesignTime;
            int actualSharedDesignTime;
            int actualCompileFlag;
            Guid actualGuid;

            Assert.Equal(VSConstants.S_OK,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out actualDesignTime, out actualSharedDesignTime, out actualCompileFlag, out actualGuid));

            Assert.Equal(designTimeSource == 1 ? 1 : 0, actualDesignTime);
            Assert.Equal(sharedDesignTimeSource == 1 ? 1 : 0, actualSharedDesignTime);
            Assert.Equal(compileFlag == 1 ? 1 : 0, actualCompileFlag);
            Assert.Equal(ResXGuid, actualGuid);
        }

        [Fact]
        public void SingleFileGeneratorFactoryAggregator_GivenValidRegistry_OptionalParamsAreOptional()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .CreateSubkey("Generators", false)
                .CreateSubkey(PackageGuid.ToString("B").ToUpper(), false)
                .CreateSubkey("ResXCodeFileGenerator", false)
                .SetValue("CLSID", ResXGuid.ToString())
                .Build();

            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService, registryHelper);

            int actualDesignTime;
            int actualSharedDesignTime;
            int actualCompileFlag;
            Guid actualGuid;

            Assert.Equal(VSConstants.S_OK,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out actualDesignTime, out actualSharedDesignTime, out actualCompileFlag, out actualGuid));

            Assert.Equal(0, actualDesignTime);
            Assert.Equal(0, actualSharedDesignTime);
            Assert.Equal(0, actualCompileFlag);
            Assert.Equal(ResXGuid, actualGuid);
        }

        [Fact]
        public void SingleFileGeneratorFactoryAggregator_NoClsid_ReturnsFail()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .CreateSubkey("Generators", false)
                .CreateSubkey(PackageGuid.ToString("B").ToUpper(), false)
                .CreateSubkey("ResXCodeFileGenerator", false)
                .Build();

            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService, registryHelper);

            int actualDesignTime;
            int actualSharedDesignTime;
            int actualCompileFlag;
            Guid actualGuid;

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out actualDesignTime, out actualSharedDesignTime, out actualCompileFlag, out actualGuid));
        }

        [Fact]
        public void SingleFileGeneratorFactoryAggregator_NoGeneratorId_ReturnsFail()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .CreateSubkey("Generators", false)
                .CreateSubkey(PackageGuid.ToString("B").ToUpper(), false)
                .Build();

            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService, registryHelper);

            int actualDesignTime;
            int actualSharedDesignTime;
            int actualCompileFlag;
            Guid actualGuid;

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out actualDesignTime, out actualSharedDesignTime, out actualCompileFlag, out actualGuid));
        }

        [Fact]
        public void SingleFileGeneratorFactoryAggregator_NoPackage_ReturnsFail()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .CreateSubkey("Generators", false)
                .Build();

            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService, registryHelper);

            int actualDesignTime;
            int actualSharedDesignTime;
            int actualCompileFlag;
            Guid actualGuid;

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out actualDesignTime, out actualSharedDesignTime, out actualCompileFlag, out actualGuid));
        }

        [Fact]
        public void SingleFileGeneratorFactoryAggregator_NoGenerators_ReturnsFail()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .Build();

            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService, registryHelper);

            int actualDesignTime;
            int actualSharedDesignTime;
            int actualCompileFlag;
            Guid actualGuid;

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out actualDesignTime, out actualSharedDesignTime, out actualCompileFlag, out actualGuid));
        }
    }
}
