using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    public class BuildEventsFake : BuildEvents
    {
        #region _dispBuildEvents_Event Members

        event _dispBuildEvents_OnBuildBeginEventHandler _dispBuildEvents_Event.OnBuildBegin
        {
            add
            { //NYI
            }
            remove
            { //NYI
            }
        }

        event _dispBuildEvents_OnBuildDoneEventHandler _dispBuildEvents_Event.OnBuildDone
        {
            add
            { //NYI
            }
            remove
            { //NYI
            }
        }

        event _dispBuildEvents_OnBuildProjConfigBeginEventHandler _dispBuildEvents_Event.OnBuildProjConfigBegin
        {
            add { throw new Exception("The method or operation is not implemented."); }
            remove { throw new Exception("The method or operation is not implemented."); }
        }

        event _dispBuildEvents_OnBuildProjConfigDoneEventHandler _dispBuildEvents_Event.OnBuildProjConfigDone
        {
            add { throw new Exception("The method or operation is not implemented."); }
            remove { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion
    }
}
