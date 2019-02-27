// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public partial class ComponentVerificationTests
    {
        // Produces inputs for theory based on the individual imports for a part
        internal class ImportsTestData : TheoryData<ComposablePartDefinition, ImportDefinitionBinding>
        {
            public ImportsTestData()
            {
                var catalog = ComponentComposition.Instance.Catalog;

                foreach (ComposablePartDefinition definition in catalog.Parts)
                {
                    foreach (ImportDefinitionBinding import in definition.Imports)
                    {
                        Add(definition, import);
                    }
                }
            }
        }
    }
}
