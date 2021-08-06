// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    internal partial class ConfigurationDimensionProvider
    {
        /// <summary>
        ///     Holds the values and source of a guessed dimension.
        /// </summary>
        private class GuessedDimensionValueBuilder : IDimensionValues
        {
            public GuessedDimensionValueBuilder(DimensionDefinition definition)
            {
                Definition = definition;
            }

            public DimensionDefinition Definition
            {
                get;
            }

            public DimensionSource Source
            {
                get;
                set;
            }

            public string? Value
            {
                get;
                set;
            }

            string? IDimensionValues.FirstValue => Value;

            IEnumerable<string> IDimensionValues.Values => Value == null ? Enumerable.Empty<string>() : new[] { Value };
        }
    }
}
