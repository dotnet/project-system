// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

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
