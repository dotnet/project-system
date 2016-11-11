using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities.ExportFactory
{
    internal static class IExportFactoryFactory
    {
        public static IExportFactory<T> CreateInstance<T>() => Mock.Of<IExportFactory<T>>();    

        public static IExportFactory<T> ImplementCreateValue<T>(Func<T> factory)
        {
            var mock = new Mock<IExportFactory<T>>();
            mock.Setup(e => e.CreateExport()).Returns(factory);
            return mock.Object;
        }
    }
}
