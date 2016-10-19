using System;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    class XmlEditorWrapper : WindowPane
    {
        private readonly WindowPane _delegatePane;

        public XmlEditorWrapper(WindowPane delegatePane)
        {
            _delegatePane = delegatePane;
        }

        public override System.Object Content
        {
            get
            {
                return _delegatePane.Content;
            }

            set
            {
                _delegatePane.Content = value;
            }
        }

        public override System.Windows.Forms.IWin32Window Window
        {
            get
            {
                return _delegatePane.Window;
            }
        }

        public override Int32 LoadUIState(Stream stateStream)
        {
            return _delegatePane.LoadUIState(stateStream);
        }

        public override Int32 SaveUIState(out Stream stateStream)
        {
            return _delegatePane.SaveUIState(out stateStream);
        }
    }
}
