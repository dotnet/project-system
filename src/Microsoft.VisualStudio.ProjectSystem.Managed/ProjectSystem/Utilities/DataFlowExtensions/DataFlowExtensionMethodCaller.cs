// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities.DataFlowExtensions
{
    /// <summary>
    /// <see cref="DataFlowExtensionMethodCaller"/> is a caller of a <seealso cref="IDataFlowExtensionWrapper"/>,
    /// which wraps a call to extension methods defined in DataFlowExtensions. These methods cannot be mocked since
    /// these are static methods. Hence we are covering the static methods with wrapper which makes the behavior non-static
    /// which will then let us mock
    /// </summary>
    internal class DataFlowExtensionMethodCaller
    {
        private IDataFlowExtensionWrapper _wrapper;

        public DataFlowExtensionMethodCaller(IDataFlowExtensionWrapper wrapper)
        {
            _wrapper = wrapper;
        }

        public IDisposable LinkTo(
            IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> sourceBlock,
            ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target,
            IEnumerable<string> ruleNames,
            bool suppressVersionOnlyUpdates)
        {
            return _wrapper.LinkTo(sourceBlock, target, ruleNames, suppressVersionOnlyUpdates);
        }
    }
}
