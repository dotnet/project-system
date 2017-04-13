using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ProjectSystemTrait]
    public class OutputTypeEnumProviderTests
    {
        [Fact]
        public async Task Constructor()
        {
            var provider = new OutputTypeEnumProvider();
            var generator = await provider.GetProviderAsync(null);

            Assert.NotNull(generator);
        }

        [Fact]
        public async Task GetListedValues()
        {
            var provider = new OutputTypeEnumProvider();
            var generator = await provider.GetProviderAsync(null);
            var values = await generator.GetListedValuesAsync();
            
            Assert.Equal(3, values.Count);
            Assert.Equal(new List<string> { "0", "1", "2" }, values.Select(v => v.DisplayName));
        }

        [Theory]
        [InlineData("WinExe", "0")]
        [InlineData("Exe", "1")]
        [InlineData("Library", "2")]
        [InlineData("AppContainerExe", "1")]
        [InlineData("WinMDObj", "2")]
        public async Task TryCreateEnumValue(string input, string expected)
        {
            var provider = new OutputTypeEnumProvider();
            var generator = await provider.GetProviderAsync(null);

            var value = await generator.TryCreateEnumValueAsync(input);

            Assert.NotNull(value);
            Assert.Equal(expected, value.DisplayName);
        }
    }
}
