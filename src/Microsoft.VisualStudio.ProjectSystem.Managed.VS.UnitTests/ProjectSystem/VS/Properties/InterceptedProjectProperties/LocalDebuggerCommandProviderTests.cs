using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class LocalDebuggerCommandProviderTests
    {
        private const string LocalDebuggerCommandPropertyName = "LocalDebuggerCommand";

        [Theory]
        [InlineData("cmd.exe")]
        [InlineData("dotnet")]
        [InlineData("notavalidexecutable")]
        public async Task LocalDebuggerCommand_ExistingCommand_ReturnsUnmodified(string existingCommand)
        {
            var properties = IProjectPropertiesFactory.CreateWithPropertyAndValue(WindowsLocalDebugger.LocalDebuggerCommandProperty, existingCommand);
            var provider = new LocalDebuggerCommandValueProvider();

            Assert.Equal(existingCommand, await provider.OnGetEvaluatedPropertyValueAsync(existingCommand, properties));
        }        

        [Fact]
        public async Task LocalDebuggerCommand_EmptyCommand_ReturnsDotnet()
        {
            var properties = IProjectPropertiesFactory.CreateWithPropertyAndValue(WindowsLocalDebugger.LocalDebuggerCommandProperty, string.Empty);
            var provider = new LocalDebuggerCommandValueProvider();

            Assert.Equal("dotnet.exe", await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, properties));
        }
    }
}
