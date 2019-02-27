// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Composition.Reflection;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public partial class ComponentVerificationTests
    {
        // Produces inputs for theory based on the individual exports for a part
        internal class ExportsTestData : TheoryData<ComposablePartDefinition, KeyValuePair<MemberRef, ExportDefinition>>
        {
            public ExportsTestData()
            {
                var catalog = ComponentComposition.Instance.Catalog;

                foreach (ComposablePartDefinition definition in catalog.Parts)
                {
                    foreach (KeyValuePair<MemberRef, ExportDefinition> export in definition.ExportDefinitions)
                    {
                        Add(definition, export);
                    }
                }
            }
        }
    }
}
