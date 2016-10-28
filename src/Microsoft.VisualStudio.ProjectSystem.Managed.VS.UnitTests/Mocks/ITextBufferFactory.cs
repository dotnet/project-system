using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text
{
    internal static class ITextBufferFactory
    {
        public static ITextBuffer Create() => Mock.Of<ITextBuffer>();

        public static ITextBuffer ImplementSnapshot(string text)
        {
            var mock = new Mock<ITextBuffer>();
            var snapshotMock = new Mock<ITextSnapshot>();
            snapshotMock.Setup(s => s.GetText()).Returns(text);
            mock.SetupGet(t => t.CurrentSnapshot).Returns(snapshotMock.Object);
            return mock.Object;
        }
    }
}
