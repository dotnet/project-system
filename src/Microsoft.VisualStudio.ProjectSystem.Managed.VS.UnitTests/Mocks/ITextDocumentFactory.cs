using Microsoft.VisualStudio.Text;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text
{
    internal static class ITextDocumentFactory
    {
        public static ITextDocument Create() => Mock.Of<ITextDocument>();

        public static ITextDocument ImplementTextBuffer(ITextBuffer buffer)
        {
            var mock = new Mock<ITextDocument>();
            mock.SetupGet(t => t.TextBuffer).Returns(buffer);
            return mock.Object;
        }
    }
}
