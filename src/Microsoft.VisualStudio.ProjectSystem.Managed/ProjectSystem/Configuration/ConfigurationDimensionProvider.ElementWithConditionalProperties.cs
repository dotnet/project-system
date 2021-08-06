// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    internal partial class ConfigurationDimensionProvider
    {
        private struct ConditionalElement<T>
        {
            private IReadOnlyDictionary<string, string>? _conditionalProperties;

            public ConditionalElement(T element, string condition)
            {
                Element = element;
                Condition = condition;
                _conditionalProperties = null;
            }

            public T Element
            {
                get;
            }

            public string Condition
            {
                get;
            }

            public IReadOnlyDictionary<string, string> ConditionalProperties
            {
                get
                {
                    if (_conditionalProperties == null)
                    {
                        if (!BuildUtilities.TryCalculateConditionalProperties(Condition, out _conditionalProperties))
                            _conditionalProperties = ImmutableDictionary<string, string>.Empty;
                    }

                    return _conditionalProperties;
                }
            }
        }
    }
}
