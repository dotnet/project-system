// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public partial class ComponentVerificationTests
    {
        // Produces inputs for theory based on the composed part
        internal class ComposedPartTestData : TheoryData<Type>
        {
            public ComposedPartTestData()
            {
                var configuration = ComponentComposition.Instance.Configuration;

                foreach (ComposedPart part in configuration.Parts)
                {
                    Add(part.Definition.Type);
                }
            }
        }
    }
}
