// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class OrderPrecedenceImportCollectionExtensions
    {
        public static void Add<T>(this OrderPrecedenceImportCollection<T> collection, T value, string? appliesTo = null, int orderPrecedence = 0)
        {
            var metadata = IOrderPrecedenceMetadataViewFactory.Create(appliesTo, orderPrecedence);

            var export = new Lazy<T, IOrderPrecedenceMetadataView>(() => value, metadata);

            collection.Add(export);
        }

        public static void Add<T>(this OrderPrecedenceExportFactoryCollection<T> collection, T value, string? appliesTo = null, int orderPrecedence = 0)
        {
            var metadata = IOrderPrecedenceMetadataViewFactory.Create(appliesTo, orderPrecedence);

            var factory = ExportFactoryFactory.ImplementCreateValueWithAutoDispose(() => value, metadata);

            collection.Add(factory);
        }
    }
}
