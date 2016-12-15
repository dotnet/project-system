// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities.ExportFactory
{
    internal static class ExportFactoryFactory
    {
        public static ExportFactory<T> CreateInstance<T>() => new ExportFactory<T>(() => new Tuple<T, Action>(default(T), () => { }));

        public static ExportFactory<T> ImplementCreateValue<T>(Func<T> factory)
        {
            return new ExportFactory<T>(() => new Tuple<T, Action>(factory(), () => { }));
        }
    }
}
