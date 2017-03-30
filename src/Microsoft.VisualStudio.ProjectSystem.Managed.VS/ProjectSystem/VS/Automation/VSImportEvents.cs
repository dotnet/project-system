using System.ComponentModel.Composition;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [Export(typeof(ImportsEvents))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VSImportEvents : ImportsEvents
    {
        [ImportingConstructor]

        public VSImportEvents()
        {
        }

#pragma warning disable 67 // These events are used through ConnectionPointContainer
        public event _dispImportsEvents_ImportAddedEventHandler ImportAdded;
        public event _dispImportsEvents_ImportRemovedEventHandler ImportRemoved;
#pragma warning restore 67
    }
}
