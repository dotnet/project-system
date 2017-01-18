using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [ProjectSystemTrait]
    public class ProjectEncodingStringWriterTests
    {
        [Fact]
        public void ProjectEncodingStringWriter_NullThreadingService_Throws()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () => new ProjectEncodingStringWriter(null, UnconfiguredProjectFactory.Create()));
        }

        [Fact]
        public void ProjectEncodingStringWriter_NullUnconfiguredProject_Throws()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => new ProjectEncodingStringWriter(IProjectThreadingServiceFactory.Create(), null));
        }

        [Theory]
        [InlineData("utf-16")]
        [InlineData("utf-8")]
        [InlineData("ascii")]
        public void ProjectEncodingStringWriter_EncodingUsesProjectEncoding(string encodingString)
        {
            var encoding = Encoding.GetEncoding(encodingString);
            var unconfiguredProject = UnconfiguredProjectFactory.Create(projectEncoding: encoding);
            var writer = new ProjectEncodingStringWriter(IProjectThreadingServiceFactory.Create(), unconfiguredProject);
            Assert.Equal(encoding, writer.Encoding);
        }

        [Fact]
        public void ProjectEncodingStringWriter_ChangedEncoding_DoesNotChangeWriter()
        {
            var encoding = Encoding.UTF8;
            Func<Task<Encoding>> encodingFunc = () => Task.FromResult(encoding);
            var unconfiguredProject = UnconfiguredProjectFactory.ImplementGetEncodingAsync(encodingFunc);
            var writer = new ProjectEncodingStringWriter(IProjectThreadingServiceFactory.Create(), unconfiguredProject);
            Assert.Equal(Encoding.UTF8, writer.Encoding);
            encoding = Encoding.Unicode;
            Assert.Equal(Encoding.UTF8, writer.Encoding);
        }
    }
}
