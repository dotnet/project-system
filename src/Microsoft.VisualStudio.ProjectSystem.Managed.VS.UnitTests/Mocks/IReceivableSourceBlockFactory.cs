using Microsoft.VisualStudio.ProjectSystem;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static Microsoft.VisualStudio.ProjectSystem.VS.Debug.StartupProjectRegistrar;

namespace Microsoft.VisualStudio.Mocks
{
        public class TestWrapperMethod : IStaticWrapper
        {
            public IDisposable LinkTo(IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> sourceBlock, ITargetBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> target, IEnumerable<string> ruleNames, bool suppressVersionOnlyUpdates)
            {
                return null;
            }
        }
}
