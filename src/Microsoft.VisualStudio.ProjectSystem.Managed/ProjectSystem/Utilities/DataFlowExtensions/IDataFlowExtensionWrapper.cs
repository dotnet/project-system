// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities.DataFlowExtensions
{
    public interface IDataFlowExtensionWrapper
    {
        IDisposable LinkTo(
            IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> sourceBlock,
            ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target,
            IEnumerable<string> ruleNames,
            bool suppressVersionOnlyUpdates);
    }
}
