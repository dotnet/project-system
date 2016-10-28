using Microsoft.VisualStudio.TextManager.Interop;
using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsPersistDocDataFactory
    {
        public static IVsPersistDocData Create() => Mock.Of<IVsPersistDocData>();

        public static IVsPersistDocData ImplementAsIVsTextBuffer()
        {
            var mock = new Mock<IVsPersistDocData>();
            mock.As<IVsTextBuffer>();
            return mock.Object;
        }
    }
}
