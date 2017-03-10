// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.MockObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Microsoft.VisualStudio.Shell.Interop;
using System.Drawing;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    public class UIServiceFake : IUIService
    {
        public IWin32Window Fake_ownerWindow = new Control();
        public Dictionary<string, object> Fake_stylesDictionary = new Dictionary<string, object>();

        public UIServiceFake()
        {
            Fake_stylesDictionary.Add("DialogFont", new Font("Arial", 8f));
        }

        #region IUIService Members

        bool IUIService.CanShowComponentEditor(object component)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        IWin32Window IUIService.GetDialogOwnerWindow()
        {
            return Fake_ownerWindow;
        }

        void IUIService.SetUIDirty()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        bool IUIService.ShowComponentEditor(object component, IWin32Window parent)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        DialogResult IUIService.ShowDialog(Form form)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void IUIService.ShowError(Exception ex, string message)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void IUIService.ShowError(Exception ex)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void IUIService.ShowError(string message)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        DialogResult IUIService.ShowMessage(string message, string caption, MessageBoxButtons buttons)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void IUIService.ShowMessage(string message, string caption)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void IUIService.ShowMessage(string message)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        bool IUIService.ShowToolWindow(Guid toolWindow)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        System.Collections.IDictionary IUIService.Styles
        {
            get
            {
                return Fake_stylesDictionary;
            }
        }

        #endregion
    }
}
