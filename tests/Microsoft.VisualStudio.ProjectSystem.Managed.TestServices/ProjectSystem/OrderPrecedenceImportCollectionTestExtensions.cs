// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class OrderPrecedenceImportCollectionTestExtensions
    {
        public static void Add<T>(this OrderPrecedenceImportCollection<T, INamedExportMetadataView> collection, string name, T item)
        {
            var mock = new Mock<INamedExportMetadataView>();
            mock.Setup(v => v.Name)
                .Returns(name);

            var result = new Lazy<T, INamedExportMetadataView>(() => item, mock.Object);

            collection.Add(result);
        }
    }
}
