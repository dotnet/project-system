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
    public class IVsSingleFileGeneratorFactoryAggregatorTests
    {
        public static Guid PackageGuid = Guid.Parse("860A27C0-B665-47F3-BC12-637E16A1050A");

        private static Guid ResXGuid = Guid.Parse(SingleFileGenerators.ResXGuid);

        [Theory]
        [InlineData(1, 0, 1)]
        [InlineData(0, 1, 0)]
        [InlineData(2, 3, 4)]
        public void IVsSingleFileGeneratorFactoryAggregator_GivenValidRegistry_RetrievesData(int designTimeSource, int sharedDesignTimeSource, int compileFlag)
        {
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

            var aggregator = new VsSingleFileGenerator(serviceProvider, registryHelper);

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
        public void IVsSingleFileGeneratorFactoryAggregator_GivenValidRegistry_OptionalParamsAreOptional()
        {
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .CreateSubkey("Generators", false)
                .CreateSubkey(PackageGuid.ToString("B").ToUpper(), false)
                .CreateSubkey("ResXCodeFileGenerator", false)
                .SetValue("CLSID", ResXGuid.ToString())
                .Build();

            var aggregator = new VsSingleFileGenerator(serviceProvider, registryHelper);

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
        public void IVsSingleFileGeneratorFactoryAggregator_NoClsid_ReturnsFail()
        {
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .CreateSubkey("Generators", false)
                .CreateSubkey(PackageGuid.ToString("B").ToUpper(), false)
                .CreateSubkey("ResXCodeFileGenerator", false)
                .Build();

            var aggregator = new VsSingleFileGenerator(serviceProvider, registryHelper);

            int actualDesignTime;
            int actualSharedDesignTime;
            int actualCompileFlag;
            Guid actualGuid;

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out actualDesignTime, out actualSharedDesignTime, out actualCompileFlag, out actualGuid));
        }

        [Fact]
        public void IVsSingleFileGeneratorFactoryAggregator_NoGeneratorId_ReturnsFail()
        {
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .CreateSubkey("Generators", false)
                .CreateSubkey(PackageGuid.ToString("B").ToUpper(), false)
                .Build();

            var aggregator = new VsSingleFileGenerator(serviceProvider, registryHelper);

            int actualDesignTime;
            int actualSharedDesignTime;
            int actualCompileFlag;
            Guid actualGuid;

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out actualDesignTime, out actualSharedDesignTime, out actualCompileFlag, out actualGuid));
        }

        [Fact]
        public void IVsSingleFileGeneratorFactoryAggregator_NoPackage_ReturnsFail()
        {
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .CreateSubkey("Generators", false)
                .Build();

            var aggregator = new VsSingleFileGenerator(serviceProvider, registryHelper);

            int actualDesignTime;
            int actualSharedDesignTime;
            int actualCompileFlag;
            Guid actualGuid;

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out actualDesignTime, out actualSharedDesignTime, out actualCompileFlag, out actualGuid));
        }

        [Fact]
        public void IVsSingleFileGeneratorFactoryAggregator_NoGenerators_ReturnsFail()
        {
            var serviceProvider = IServiceProviderFactory.Create();
            var registryHelper = new IVSRegistryHelperBuilder().CreateHive(__VsLocalRegistryType.RegType_Configuration, false)
                .Build();

            var aggregator = new VsSingleFileGenerator(serviceProvider, registryHelper);

            int actualDesignTime;
            int actualSharedDesignTime;
            int actualCompileFlag;
            Guid actualGuid;

            Assert.Equal(VSConstants.E_FAIL,
                aggregator.GetGeneratorInformation("ResXCodeFileGenerator", out actualDesignTime, out actualSharedDesignTime, out actualCompileFlag, out actualGuid));
        }
    }

    internal class VsSingleFileGenerator : IVsSingleFileGeneratorFactoryAggregator
    {
        public VsSingleFileGenerator(IServiceProvider serviceProvider, IVSRegistryHelper registryHelper) : base(serviceProvider, registryHelper)
        {
        }

        protected override Guid PackageGuid => IVsSingleFileGeneratorFactoryAggregatorTests.PackageGuid;
    }
}
