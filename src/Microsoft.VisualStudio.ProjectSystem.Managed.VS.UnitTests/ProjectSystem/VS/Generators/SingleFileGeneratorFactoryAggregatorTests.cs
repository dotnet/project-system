using System;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Xunit;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [Trait("UnitTest", "ProjectSystem")]
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
            var manager = CreateManager(true, designTimeSource, sharedDesignTimeSource, compileFlag);
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => manager);

            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService);

            Assert.Equal(VSConstants.S_OK,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));

            Assert.Equal(designTimeSource == 1 ? 1 : 0, actualDesignTime);
            Assert.Equal(sharedDesignTimeSource == 1 ? 1 : 0, actualSharedDesignTime);
            Assert.Equal(compileFlag == 1 ? 1 : 0, actualCompileFlag);
            Assert.Equal(ResXGuid, actualGuid);
        }

        [Fact]
        public void SingleFileGeneratorFactoryAggregator_GivenValidRegistry_OptionalParamsAreOptional()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var manager = CreateManager();
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => manager);
            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService);

            Assert.Equal(VSConstants.S_OK,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));

            Assert.Equal(0, actualDesignTime);
            Assert.Equal(0, actualSharedDesignTime);
            Assert.Equal(0, actualCompileFlag);
            Assert.Equal(ResXGuid, actualGuid);
        }

        [Fact]
        public void SingleFileGeneratorFactoryAggregator_NoClsid_ReturnsFail()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var manager = CreateManagerForPath($"Generators\\{PackageGuid.ToString("B").ToUpper()}\\ResXCodeFileGenerator");
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => manager);
            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService);

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));
        }

        [Fact]
        public void SingleFileGeneratorFactoryAggregator_NoGeneratorId_ReturnsFail()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var manager = CreateManagerForPath($"Generators\\{PackageGuid.ToString("B").ToUpper()}");
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => manager);
            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService);

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));
        }

        [Fact]
        public void SingleFileGeneratorFactoryAggregator_NoPackage_ReturnsFail()
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var manager = CreateManagerForPath($"Generators");
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => manager);

            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService);

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));
        }

        [Fact]
        public void SingleFileGeneratorFactoryAggregator_NoGenerators_ReturnsFail()
        {
            UnitTestHelper.IsRunningUnitTests = true;
             var manager = CreateManagerForPath("");
            var serviceProvider = IServiceProviderFactory.ImplementGetService(type => manager);

            var integrationService = IVsUnconfiguredProjectIntegrationServiceFactory.ImplementProjectTypeGuid(PackageGuid);

            var aggregator = new SingleFileGeneratorFactoryAggregator(serviceProvider, integrationService);

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out int actualDesignTime, out int actualSharedDesignTime, out int actualCompileFlag, out Guid actualGuid));
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
            return CreateManagerForPath($"Generators\\{PackageGuid.ToString("B").ToUpper()}\\ResXCodeFileGenerator", vals);
        }

        private IVsSettingsManager CreateManagerForPath(string path, IDictionary<string, object> vals = null)
        {
            var store = new IVsSettingsStoreTester
            {
                Keys = new Dictionary<string, IDictionary<string, object>>(StringComparer.OrdinalIgnoreCase)
                {
                    { path, vals ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) }
                }
            };
            return new IVsSettingsManagerFactory
            {
                Stores = new Dictionary<uint, IVsSettingsStore>
                {
                    { (uint)__VsSettingsScope.SettingsScope_Configuration, store }
                }
            };
        }
    }
}
