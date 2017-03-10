// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Editors.MyApplication;
using System.ComponentModel;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    public class MyApplicationPropertiesFake : IMyApplicationPropertiesInternal, INotifyPropertyChanged
    {

        public int Fake_AuthenticationMode = 0;
        public bool Fake_CustomSubMainRaw = false;
        public bool Fake_EnableVisualStyles = true;
        public string Fake_MainForm = "WindowsApplication1.Form1";
        public string Fake_MainFormNoRootNamespace = "Form1";
        public bool Fake_SaveMySettingsOnExit = true;
        public int Fake_ShutdownMode = 0;
        public bool Fake_SingleInstance = true;
        public string Fake_SplashScreen = "";
        public string Fake_SplashScreenNoRootNamespace = "";
        public int Fake_cRunCustomTool = 0;

        #region IVsMyApplicationProperties Members

        int IVsMyApplicationProperties.AuthenticationMode
        {
            get
            {
                return Fake_AuthenticationMode;
            }
            set
            {
                Fake_AuthenticationMode = value;
                //RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(PropertyName))
            }
        }

        bool IVsMyApplicationProperties.CustomSubMain
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                Fake_CustomSubMainRaw = value;
            }
        }

        bool IVsMyApplicationProperties.EnableVisualStyles
        {
            get
            {
                return Fake_EnableVisualStyles;
            }
            set
            {
                Fake_EnableVisualStyles = value;
            }
        }

        string IVsMyApplicationProperties.MainForm
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool IVsMyApplicationProperties.SaveMySettingsOnExit
        {
            get
            {
                return Fake_SaveMySettingsOnExit;
            }
            set
            {
                Fake_SaveMySettingsOnExit = value;
            }
        }

        int IVsMyApplicationProperties.ShutdownMode
        {
            get
            {
                return Fake_ShutdownMode;
            }
            set
            {
                Fake_ShutdownMode = value;
            }
        }

        bool IVsMyApplicationProperties.SingleInstance
        {
            get
            {
                return Fake_SingleInstance;
            }
            set
            {
                Fake_SingleInstance = value;
            }
        }

        string IVsMyApplicationProperties.SplashScreen
        {
            get
            {
                return Fake_SplashScreen;
            }
            set
            {
                //Fake_SplashScreen = value;
                throw new NotImplementedException(); //Consider NoRootNamespace version...
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                //NYI
            }
            remove
            {
                //NYI
            }
        }

        #endregion

        #region IMyApplicationPropertiesInternal Members

        bool IMyApplicationPropertiesInternal.CustomSubMainRaw
        {
            get { return Fake_CustomSubMainRaw; }
        }

        string IMyApplicationPropertiesInternal.MainFormNoRootNamespace
        {
            get
            {
                return Fake_MainFormNoRootNamespace;
            }
            set
            {
                Fake_MainFormNoRootNamespace = value;
            }
        }

        void IMyApplicationPropertiesInternal.NavigateToEvents()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void IMyApplicationPropertiesInternal.RunCustomTool()
        {
            ++Fake_cRunCustomTool;
        }

        string IMyApplicationPropertiesInternal.SplashScreenNoRootNS
        {
            get
            {
                return Fake_SplashScreenNoRootNamespace;
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        #endregion
    }
}
