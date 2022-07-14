// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace System.ComponentModel.Composition
{
    internal static class ExportFactoryFactory
    {
        public static ExportFactory<T> Implement<T>(Func<T> factory, Action? disposeAction = null)
        {
            return new ExportFactory<T>(() =>
            {
                T value = factory();

                return Tuple.Create(value, disposeAction ?? delegate { });
            });
        }

        public static ExportFactory<T> ImplementCreateValueWithAutoDispose<T>(Func<T> factory)
        {
            return new ExportFactory<T>(() =>
            {
                T value = factory();

                return Tuple.Create(value, () =>
                {
                    if (value is IDisposable disposable)
                        disposable.Dispose();
                });
            });
        }

        public static ExportFactory<T, TMetadata> ImplementCreateValueWithAutoDispose<T, TMetadata>(Func<T> factory, TMetadata metadata)
        {
            return new ExportFactory<T, TMetadata>(() =>
            {
                T value = factory();

                return Tuple.Create(value, () =>
                {
                    if (value is IDisposable disposable)
                        disposable.Dispose();
                });
            }, metadata);
        }
    }
}
