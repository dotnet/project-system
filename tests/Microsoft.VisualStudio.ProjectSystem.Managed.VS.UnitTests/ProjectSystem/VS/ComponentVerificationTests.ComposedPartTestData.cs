// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

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
