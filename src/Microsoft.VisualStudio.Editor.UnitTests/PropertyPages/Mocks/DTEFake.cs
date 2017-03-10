// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    [CLSCompliant(false)]
    public class DTEFake : DTE
    {
        public EventsFake Fake_events = new EventsFake();

        #region _DTE Members

        Document _DTE.ActiveDocument
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object _DTE.ActiveSolutionProjects
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Window _DTE.ActiveWindow
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        AddIns _DTE.AddIns
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        DTE _DTE.Application
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object _DTE.CommandBars
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string _DTE.CommandLineArguments
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Commands _DTE.Commands
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        ContextAttributes _DTE.ContextAttributes
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        DTE _DTE.DTE
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Debugger _DTE.Debugger
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        vsDisplay _DTE.DisplayMode
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        Documents _DTE.Documents
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string _DTE.Edition
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Events _DTE.Events
        {
            get
            {
                return Fake_events;
            }
        }

        void _DTE.ExecuteCommand(string CommandName, string CommandArgs)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        string _DTE.FileName
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Find _DTE.Find
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string _DTE.FullName
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object _DTE.GetObject(string Name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        Globals _DTE.Globals
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        ItemOperations _DTE.ItemOperations
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        wizardResult _DTE.LaunchWizard(string VSZFile, ref object[] ContextParams)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int _DTE.LocaleID
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Macros _DTE.Macros
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        DTE _DTE.MacrosIDE
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Window _DTE.MainWindow
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        vsIDEMode _DTE.Mode
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string _DTE.Name
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        ObjectExtenders _DTE.ObjectExtenders
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Window _DTE.OpenFile(string ViewKind, string FileName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void _DTE.Quit()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        string _DTE.RegistryRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string _DTE.SatelliteDllPath(string Path, string Name)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        SelectedItems _DTE.SelectedItems
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Solution _DTE.Solution
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        SourceControl _DTE.SourceControl
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        StatusBar _DTE.StatusBar
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        bool _DTE.SuppressUI
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        UndoContext _DTE.UndoContext
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        bool _DTE.UserControl
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string _DTE.Version
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        WindowConfigurations _DTE.WindowConfigurations
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        Windows _DTE.Windows
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        bool _DTE.get_IsOpenFile(string ViewKind, string FileName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        Properties _DTE.get_Properties(string Category, string Page)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
