using Microsoft.VisualStudio.ProjectSystem;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    public class IProjectValueDataSourceFactory
    {
        public static IProjectValueDataSource<T> CreateInstance<T>()
        {
            var mock = new Mock<IProjectValueDataSource<T>>();
            return mock.Object;
        }
    }
}
