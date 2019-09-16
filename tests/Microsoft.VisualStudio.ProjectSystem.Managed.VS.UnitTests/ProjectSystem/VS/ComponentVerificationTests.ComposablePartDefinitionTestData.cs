// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public partial class ComponentVerificationTests
    {
        internal class ComposablePartDefinitionTestData : TheoryData<Type>
        {
            public ComposablePartDefinitionTestData()
            {
                var catalog = ComponentComposition.Instance.Catalog;

                foreach (ComposablePartDefinition part in catalog.Parts)
                {
                    Add(part.Type);
                }
            }
        }
    }
}
