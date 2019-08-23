// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal partial class ComponentComposition
    {
        public class ContractMetadata
        {
            public ProjectSystemContractProvider? Provider { get; set; }

            public ProjectSystemContractScope? Scope { get; set; }

            public ImportCardinality? Cardinality { get; set; }
        }
    }
}
