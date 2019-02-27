// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class ComposablePartDefinitionTestData : TheoryData<ComposablePartDefinition>
    {
        public ComposablePartDefinitionTestData()
        {
            var catalog = ComponentComposition.Instance.Catalog;

            foreach (ComposablePartDefinition part in catalog.Parts)
            {
                Add(part);
            }
        }
    }

    internal class ComposedPartTestData : TheoryData<ComposedPart>
    {
        public ComposedPartTestData()
        {
            var configuration = ComponentComposition.Instance.Configuration;

            foreach (ComposedPart part in configuration.Parts)
            {
                Add(part);
            }
        }
    }
}
