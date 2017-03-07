using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    [CLSCompliant(false)]
    public class EventsFake : Events
    {
        public BuildEvents Fake_buildEvents = new BuildEventsFake();

        #region Events Members

        BuildEvents Events.BuildEvents
        {
            get
            {
                return Fake_buildEvents;
            }
        }

        DTEEvents Events.DTEEvents
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        DebuggerEvents Events.DebuggerEvents
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        FindEvents Events.FindEvents
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object Events.GetObject(string Name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        ProjectItemsEvents Events.MiscFilesEvents
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        SelectionEvents Events.SelectionEvents
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        SolutionEvents Events.SolutionEvents
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        ProjectItemsEvents Events.SolutionItemsEvents
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object Events.get_CommandBarEvents(object CommandBarControl)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        CommandEvents Events.get_CommandEvents(string Guid, int ID)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        DocumentEvents Events.get_DocumentEvents(Document Document)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        OutputWindowEvents Events.get_OutputWindowEvents(string Pane)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        TaskListEvents Events.get_TaskListEvents(string Filter)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        TextEditorEvents Events.get_TextEditorEvents(TextDocument TextDocumentFilter)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        WindowEvents Events.get_WindowEvents(Window WindowFilter)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
