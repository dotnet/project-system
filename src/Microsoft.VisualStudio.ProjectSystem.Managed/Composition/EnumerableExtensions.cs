// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.Composition
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Enumerates a sequence of extensions, omitting any extensions that throw MEF exceptions.
        /// </summary>
        /// <typeparam name="T">The type of extension.</typeparam>
        /// <param name="extensions">The collection of extensions.</param>
        /// <param name="onlyCreatedValues">
        /// <c>true</c> to only enumerate extensions from Lazy's that have previously been created.
        /// This is useful in Dispose methods to avoid MEF ObjectDisposedExceptions from accidentally
        /// creating values during a container disposal.
        /// </param>
        /// <returns>The safely constructed sequence of extensions.</returns>
        internal static IEnumerable<T> ExtensionValues<T>(this IEnumerable<Lazy<T>> extensions, bool onlyCreatedValues = false)
        {
            Requires.NotNull(extensions, nameof(extensions));
            string traceErrorMessage = "Roslyn project system extension rejected due to exception: {0}";

            foreach (Lazy<T> extension in extensions)
            {
                T value;
                try
                {
                    if (onlyCreatedValues && !extension.IsValueCreated)
                    {
                        continue;
                    }

                    value = extension.Value;
                }
                catch (CompositionContractMismatchException ex)
                {
                    TraceUtilities.TraceError(traceErrorMessage, ex);
                    continue;
                }
                catch (CompositionException ex)
                {
                    TraceUtilities.TraceError(traceErrorMessage, ex);
                    continue;
                }

                yield return value;
            }
        }

        /// <summary>
        /// Enumerates a sequence of extensions, omitting any extensions that throw MEF exceptions.
        /// </summary>
        /// <typeparam name="T">The type of extension.</typeparam>
        /// <typeparam name="TMetadata">The metadata on each extension.</typeparam>
        /// <param name="extensions">The collection of extensions.</param>
        /// <param name="onlyCreatedValues">
        /// <c>true</c> to only enumerate extensions from Lazy's that have previously been created.
        /// This is useful in Dispose methods to avoid MEF ObjectDisposedExceptions from accidentally
        /// creating values during a container disposal.
        /// </param>
        /// <returns>The safely constructed sequence of extensions.</returns>
        internal static IEnumerable<T> ExtensionValues<T, TMetadata>(this IEnumerable<Lazy<T, TMetadata>> extensions, bool onlyCreatedValues = false)
        {
            IEnumerable<Lazy<T>> simpleSequence = extensions;
            return ExtensionValues(simpleSequence, onlyCreatedValues);
        }
    }
}
