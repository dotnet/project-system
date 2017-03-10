// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Editors;
using Microsoft.VisualStudio.Editors.DesignerFramework;
using Microsoft.VisualStudio.Editors.ResourceEditor;

using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Editors.UnitTests.DesignerFramework;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using System.CodeDom.Compiler;
using Microsoft.VisualBasic;
using Microsoft.CSharp;
using Microsoft.VJSharp;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Editors.ApplicationDesigner;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using System.Drawing;

namespace Microsoft.VisualStudio.Editors.UnitTests.SettingsDesigner
{
    [TestClass]
    public class ApplicationDesignerPanelTests
    {
        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void CreateDesignerWhenPropertyPageInfoTryLoadPropertyPageFailsSoSiteIsNothing_DevDivBugs17865()
        {
            SequenceMock<IVsWindowFrame> outerWindowFrameMock = new SequenceMock<IVsWindowFrame>(typeof(IVsWindowFrame2));
            SequenceMock<IVsWindowFrame> innerWindowFrameMock = new SequenceMock<IVsWindowFrame>(typeof(IVsWindowFrame2));

            Control parentControl = new Control();
            parentControl.Size = new Size(10, 10);
            ServiceProviderMock spMock = new ServiceProviderMock();
            spMock.Fake_AddService(typeof(IVsWindowFrame), outerWindowFrameMock.Instance);
            UIShellService2FakeWithColors shellServiceFake = new UIShellService2FakeWithColors();
            spMock.Fake_AddService(typeof(IVsUIShell), shellServiceFake);
            VsShellFake shellFake = new VsShellFake();
            spMock.Fake_AddService(typeof(IVsShell), shellFake);
            UIServiceFake uiServiceFake = new UIServiceFake();
            spMock.Fake_AddService(typeof(IUIService), uiServiceFake);
            SequenceMock<IVsUIShellOpenDocument> uiShellOpenDocumentMock = new SequenceMock<IVsUIShellOpenDocument>();
            spMock.Fake_AddService(typeof(IVsUIShellOpenDocument), uiShellOpenDocumentMock.Instance);
            Mock<OLE.Interop.IServiceProvider> oleServiceProviderMock = new Mock<Microsoft.VisualStudio.OLE.Interop.IServiceProvider>();
            spMock.Fake_AddService(typeof(OLE.Interop.IServiceProvider), oleServiceProviderMock.Instance);
            ApplicationDesignerView view = new ApplicationDesignerView(spMock.Instance);
            IVsHierarchy hierarchy = new Mock<IVsHierarchy>(typeof(IVsUIHierarchy)).Instance;
            Guid guid = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
            Mock<PropertyPageInfo> infoMock = new Mock<PropertyPageInfo>();
            infoMock.SetCreateArguments(view, guid, false);
            infoMock.Implement("TryLoadPropertyPage"); // TryLoadPropertyPage is a NOOP, so Site will remain NULL
            uint itemid = 123;
            ApplicationDesignerPanelFake panel = new ApplicationDesignerPanelFake(view, hierarchy, itemid, infoMock.Instance);
            parentControl.Controls.Add(panel);
            panel.MkDocument = "my moniker";

            // Fail the IsDocumentInAProject call
            uiShellOpenDocumentMock.AddExpectation("IsDocumentInAProject",
                //string pszMkDocument, out IVsUIHierarchy ppUIH, out uint pitemid, out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP, out int pDocInProj)
                new object[] { MockConstraint.IsAnything<string>(),
                    null,
                    (uint)0,
                    null,
                    0},
                new object[] { null,
                    null,
                    (uint)0,
                    null,
                    0},
                VSConstants.S_OK);

            // OpenSpecificEditor call should succeed

            uiShellOpenDocumentMock.AddExpectation("OpenSpecificEditor",
                //int OpenSpecificEditor(uint grfOpenSpecific, string pszMkDocument, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, string pszOwnerCaption, IVsUIHierarchy pHier, uint itemid, IntPtr punkDocDataExisting, Microsoft.VisualStudio.OLE.Interop.IServiceProvider pSPHierContext, out IVsWindowFrame ppWindowFrame);
                new object[] {
                    MockConstraint.IsAnything<uint>(), //uint grfOpenSpecific
                    MockConstraint.IsAnything<string>(), //string pszMkDocument
                    MockConstraint.IsAnything<Guid>(),   //ref Guid rguidEditorType
                    MockConstraint.IsAnything<string>(), //string pszPhysicalView
                    MockConstraint.IsAnything<Guid>(),   //ref Guid rguidLogicalView
                    MockConstraint.IsAnything<string>(), //string pszOwnerCaption
                    MockConstraint.IsAnything<IVsUIHierarchy>(), //IVsUIHierarchy pHier
                    MockConstraint.IsAnything<uint>(), //uint itemid
                    MockConstraint.IsAnything<IntPtr>(),//IntPtr punkDocDataExisting
                    MockConstraint.IsAnything<OLE.Interop.IServiceProvider>(), //OLE.Interop.IServiceProvider> pSPHierContext
                    null //out IVsWindowFrame ppWindowFrame
                },
                new object[] {
                    (uint)0, //uint grfOpenSpecific
                    null, //string pszMkDocument
                    Guid.Empty,   //ref Guid rguidEditorType
                    null, //string pszPhysicalView
                    Guid.Empty,   //ref Guid rguidLogicalView
                    null, //string pszOwnerCaption
                    null, //IVsUIHierarchy pHier
                    (uint)0, //uint itemid
                    IntPtr.Zero,//IntPtr punkDocDataExisting
                    null, //OLE.Interop.IServiceProvider> pSPHierContext
                    innerWindowFrameMock.Instance //out IVsWindowFrame ppWindowFrame
                },
                VSConstants.S_OK);

            innerWindowFrameMock.Implement("GetProperty",
                new object[] { (int)__VSFPROPID.VSFPROPID_Hierarchy, null },
                new object[] { (int)0, hierarchy },
                VSConstants.S_OK);
            innerWindowFrameMock.Implement("GetProperty",
                new object[] { (int)__VSFPROPID.VSFPROPID_ItemID, null },
                new object[] { (int)0, 321 },
                VSConstants.S_OK);

            innerWindowFrameMock.Implement("GetProperty",
                new object[] { (int)__VSFPROPID2.VSFPROPID_ParentHwnd, null },
                new object[] { (int)0, (int)0 },
                VSConstants.S_OK);

            innerWindowFrameMock.AddExpectation("SetProperty",
                new object[] { (int)__VSFPROPID2.VSFPROPID_ParentHwnd, MockConstraint.IsAnything<object>() },
                VSConstants.S_OK);
            innerWindowFrameMock.AddExpectation("SetProperty",
                new object[] { (int)__VSFPROPID2.VSFPROPID_ParentFrame, MockConstraint.IsAnything<object>() },
                VSConstants.S_OK);

            innerWindowFrameMock.Implement(new MethodId(typeof(IVsWindowFrame2), "Advise"),
                new object[] { MockConstraint.IsAnything<IVsWindowFrameNotify>(), MockConstraint.IsAnything<uint>() },
                new object[] { null, (uint)432 },
                VSConstants.S_OK);
            innerWindowFrameMock.Implement(new MethodId(typeof(IVsWindowFrame2), "Unadvise"),
                new object[] { (uint)432 },
                VSConstants.S_OK);

            innerWindowFrameMock.AddExpectation("GetProperty",
                new object[] { (int)__VSFPROPID.VSFPROPID_DocData, null },
                new object[] { (int)0, null },
                VSConstants.S_OK);
            innerWindowFrameMock.AddExpectation("GetProperty",
                new object[] { (int)__VSFPROPID.VSFPROPID_DocCookie, null },
                new object[] { (int)0, (uint)678 },
                VSConstants.S_OK);
            innerWindowFrameMock.AddExpectation("GetProperty",
                new object[] { (int)__VSFPROPID.VSFPROPID_DocView, null },
                new object[] { (int)0, null },
                VSConstants.S_OK);
            innerWindowFrameMock.Implement("GetProperty",
                new object[] { (int)__VSFPROPID.VSFPROPID_EditorCaption, null },
                new object[] { (int)0, null },
                VSConstants.S_OK);

            panel.CreateDesigner();

            //Verify
            uiShellOpenDocumentMock.Verify();
            innerWindowFrameMock.Verify();
            outerWindowFrameMock.Verify();

            Assert.IsTrue(panel.Fake_wasShowWindowFrameCalled);
        }



        private class UIShellService2FakeWithColors : IVsUIShell2, IVsUIShell
        {
            #region IVsUIShell2 Members

            int IVsUIShell2.CreateGlyphImageButton(IntPtr hwnd, ushort chGlyph, int xShift, int yShift, uint bwiPos, out IVsImageButton ppImageButton)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell2.CreateGradient(uint GRADIENTTYPE, out IVsGradient pGradient)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell2.CreateIconImageButton(IntPtr hwnd, IntPtr hicon, uint bwiPos, out IVsImageButton ppImageButton)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell2.GetDirectoryViaBrowseDlgEx(VSBROWSEINFOW[] pBrowse, string pszHelpTopic, string pszOpenButtonLabel, string pszCeilingDir, VSNSEBROWSEINFOW[] pNSEBrowseInfo)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell2.GetOpenFileNameViaDlgEx(VSOPENFILENAMEW[] pOpenFileName, string pszHelpTopic)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell2.GetSaveFileNameViaDlgEx(VSSAVEFILENAMEW[] pSaveFileName, string pszHelpTopic)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell2.GetVSCursor(uint cursor, out IntPtr phIcon)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell2.GetVSSysColorEx(int dwSysColIndex, out uint pdwRGBval)
            {
                pdwRGBval = 0x12345678;
                return VSConstants.S_OK;
            }

            int IVsUIShell2.IsAutoRecoverSavingCheckpoints(out int pfARSaving)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell2.SaveItemsViaDlg(uint cItems, VSSAVETREEITEM[] rgSaveItems)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell2.VsDialogBoxParam(uint hinst, uint dwId, uint lpDialogFunc, int lp)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            #endregion

            #region IVsUIShell Members

            int IVsUIShell.AddNewBFNavigationItem(IVsWindowFrame pWindowFrame, string bstrData, object punk, int fReplaceCurrent)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.CenterDialogOnWindow(IntPtr hwndDialog, IntPtr hwndParent)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.CreateDocumentWindow(uint grfCDW, string pszMkDocument, IVsUIHierarchy pUIH, uint itemid, IntPtr punkDocView, IntPtr punkDocData, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidCmdUI, Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp, string pszOwnerCaption, string pszEditorCaption, int[] pfDefaultPosition, out IVsWindowFrame ppWindowFrame)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.CreateToolWindow(uint grfCTW, uint dwToolWindowId, object punkTool, ref Guid rclsidTool, ref Guid rguidPersistenceSlot, ref Guid rguidAutoActivate, Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp, string pszCaption, int[] pfDefaultPosition, out IVsWindowFrame ppWindowFrame)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.EnableModeless(int fEnable)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.FindToolWindow(uint grfFTW, ref Guid rguidPersistenceSlot, out IVsWindowFrame ppWindowFrame)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.FindToolWindowEx(uint grfFTW, ref Guid rguidPersistenceSlot, uint dwToolWinId, out IVsWindowFrame ppWindowFrame)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetAppName(out string pbstrAppName)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetCurrentBFNavigationItem(out IVsWindowFrame ppWindowFrame, out string pbstrData, out object ppunk)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetDialogOwnerHwnd(out IntPtr phwnd)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetDirectoryViaBrowseDlg(VSBROWSEINFOW[] pBrowse)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetDocumentWindowEnum(out IEnumWindowFrames ppenum)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetErrorInfo(out string pbstrErrText)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetNextBFNavigationItem(out IVsWindowFrame ppWindowFrame, out string pbstrData, out object ppunk)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetOpenFileNameViaDlg(VSOPENFILENAMEW[] pOpenFileName)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetPreviousBFNavigationItem(out IVsWindowFrame ppWindowFrame, out string pbstrData, out object ppunk)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetSaveFileNameViaDlg(VSSAVEFILENAMEW[] pSaveFileName)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetToolWindowEnum(out IEnumWindowFrames ppenum)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetURLViaDlg(string pszDlgTitle, string pszStaticLabel, string pszHelpTopic, out string pbstrURL)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.GetVSSysColor(VSSYSCOLOR dwSysColIndex, out uint pdwRGBval)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.OnModeChange(DBGMODE dbgmodeNew)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.PostExecCommand(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, ref object pvaIn)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.PostSetFocusMenuCommand(ref Guid pguidCmdGroup, uint nCmdID)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.RefreshPropertyBrowser(int dispid)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.RemoveAdjacentBFNavigationItem(RemoveBFDirection rdDir)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.RemoveCurrentNavigationDupes(RemoveBFDirection rdDir)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.ReportErrorInfo(int hr)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.SaveDocDataToFile(VSSAVEFLAGS grfSave, object pPersistFile, string pszUntitledPath, out string pbstrDocumentNew, out int pfCanceled)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.SetErrorInfo(int hr, string pszDescription, uint dwReserved, string pszHelpKeyword, string pszSource)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.SetForegroundWindow()
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.SetMRUComboText(ref Guid pguidCmdGroup, uint dwCmdID, string lpszText, int fAddToList)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.SetMRUComboTextW(Guid[] pguidCmdGroup, uint dwCmdID, string pwszText, int fAddToList)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.SetToolbarVisibleInFullScreen(Guid[] pguidCmdGroup, uint dwToolbarId, int fVisibleInFullScreen)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.SetWaitCursor()
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.SetupToolbar(IntPtr hwnd, IVsToolWindowToolbar ptwt, out IVsToolWindowToolbarHost pptwth)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.ShowContextMenu(uint dwCompRole, ref Guid rclsidActive, int nMenuId, POINTS[] pos, Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget pCmdTrgtActive)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.ShowMessageBox(uint dwCompRole, ref Guid rclsidComp, string pszTitle, string pszText, string pszHelpFile, uint dwHelpContextID, OLEMSGBUTTON msgbtn, OLEMSGDEFBUTTON msgdefbtn, OLEMSGICON msgicon, int fSysAlert, out int pnResult)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.TranslateAcceleratorAsACmd(Microsoft.VisualStudio.OLE.Interop.MSG[] pMsg)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.UpdateCommandUI(int fImmediateUpdate)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsUIShell.UpdateDocDataIsDirtyFeedback(uint docCookie, int fDirty)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            #endregion

        }

        private class VsShellFake : IVsShell
        {
            #region IVsShell Members

            int IVsShell.AdviseBroadcastMessages(IVsBroadcastMessageEvents pSink, out uint pdwCookie)
            {
                pdwCookie = 1;
                return VSConstants.S_OK;
            }

            int IVsShell.AdviseShellPropertyChanges(IVsShellPropertyEvents pSink, out uint pdwCookie)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsShell.GetPackageEnum(out IEnumPackages ppenum)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsShell.GetProperty(int propid, out object pvar)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsShell.IsPackageInstalled(ref Guid guidPackage, out int pfInstalled)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsShell.IsPackageLoaded(ref Guid guidPackage, out IVsPackage ppPackage)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsShell.LoadPackage(ref Guid guidPackage, out IVsPackage ppPackage)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsShell.LoadPackageString(ref Guid guidPackage, uint resid, out string pbstrOut)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsShell.LoadUILibrary(ref Guid guidPackage, uint dwExFlags, out uint phinstOut)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsShell.SetProperty(int propid, object var)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            int IVsShell.UnadviseBroadcastMessages(uint dwCookie)
            {
                return VSConstants.S_OK;
            }

            int IVsShell.UnadviseShellPropertyChanges(uint dwCookie)
            {
                throw new Exception("The method or operation is not implemented.");
            }

            #endregion
        }

        private class ApplicationDesignerPanelFake : ApplicationDesignerPanel
        {
            public bool Fake_wasShowWindowFrameCalled;

            public ApplicationDesignerPanelFake(ApplicationDesignerView view, IVsHierarchy hierarchy, uint itemid, PropertyPageInfo info)
                : base(view, hierarchy, itemid, info)
            {
            }

            protected override void CloseFrameInternal(IVsWindowFrame WindowFrame, __FRAMECLOSE flags)
            {
                //NOOP
            }

            protected override void ShowWindowFrame()
            {
                Fake_wasShowWindowFrameCalled = true;
            }
        }

    }
}

