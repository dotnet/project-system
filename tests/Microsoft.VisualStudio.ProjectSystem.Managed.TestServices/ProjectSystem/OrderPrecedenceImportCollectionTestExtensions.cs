// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

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
