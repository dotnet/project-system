using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal class IProjectTreeServiceFactory
    {
        public static IProjectTreeService Create(IProjectTree tree)
        {
            var mock =  new Mock<IProjectTreeService>();
            
            var treeStateMock = new Mock<IProjectTreeServiceState>();
            treeStateMock.SetupGet(state => state.Tree)
                         .Returns(tree);
            treeStateMock.SetupGet(state => state.TreeProvider)
                         .Returns(IProjectTreeProviderFactory.CreateWithGetPath());

            mock.SetupGet(s => s.CurrentTree)
                .Returns(treeStateMock.Object);

            return mock.Object;
        }
    }
}
