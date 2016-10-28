using Moq;

namespace Microsoft.VisualStudio.Text
{
    internal static class ITextDocumentFactoryServiceFactory
    {
        public static ITextDocumentFactoryService Create() => Mock.Of<ITextDocumentFactoryService>();

        public static ITextDocumentFactoryService ImplementGetTextDocument(ITextDocument doc, bool success)
        {
            var mock = new Mock<ITextDocumentFactoryService>();
            mock.Setup(t => t.TryGetTextDocument(It.IsAny<ITextBuffer>(), out doc)).Returns(success);
            return mock.Object;
        }
    }
}
