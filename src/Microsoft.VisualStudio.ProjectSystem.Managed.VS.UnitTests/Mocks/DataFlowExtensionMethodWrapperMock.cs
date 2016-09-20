// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Utilities.DataFlowExtensions;

namespace Microsoft.VisualStudio.Mocks
{
    public class DataFlowExtensionMethodWrapperMock : IDataFlowExtensionWrapper
    {
        public IDisposable LinkTo(
            IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> sourceBlock,
            ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target,
            IEnumerable<string> ruleNames,
            bool suppressVersionOnlyUpdates)
        {
            return null;
        }
    }
}
