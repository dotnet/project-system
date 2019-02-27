// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public partial class ComponentVerificationTests
    {
        // Produces inputs for theory based on the individual import/export bindings for a part
        internal class SatisfyingExportsTestData : TheoryData<ComposedPart, KeyValuePair<ImportDefinitionBinding, IReadOnlyList<ExportDefinitionBinding>>>
        {
            public SatisfyingExportsTestData()
            {
                var configuration = ComponentComposition.Instance.Configuration;

                foreach (ComposedPart part in configuration.Parts)
                {
                    foreach (KeyValuePair<ImportDefinitionBinding, IReadOnlyList<ExportDefinitionBinding>> binding in part.SatisfyingExports)
                    {
                        Add(part, binding);
                    }
                }
            }
        }
    }
}
