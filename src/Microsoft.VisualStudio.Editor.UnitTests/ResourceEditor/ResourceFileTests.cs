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
using Microsoft.VisualStudio.Designer.Interfaces;
using System.Windows.Forms.Design;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.Editors.UnitTests.ResourceEditor
{
    [TestClass]
    public class ResourceFileTests
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
        public void VsWhidbey604087()
        {
            // Make sure that an exception during a flush doesn't crash
            //   VS.

            //Prepare
            const string basePath = "c:\\temp\\Fake\\";

            ServiceProviderMock spMock = new ServiceProviderMock();
            spMock.Fake_AddUiServiceFake();
            Mock<IComponentChangeService> componentChangeServiceMock = new Mock<IComponentChangeService>();
            componentChangeServiceMock.Implement("add_ComponentChanged",
                new object[] { MockConstraint.IsAnything<ComponentChangedEventHandler>() });
            componentChangeServiceMock.Implement("add_ComponentRename",
                new object[] { MockConstraint.IsAnything<ComponentRenameEventHandler>() });
            componentChangeServiceMock.Implement("add_ComponentRemoved",
                new object[] { MockConstraint.IsAnything<ComponentEventHandler>() });
            componentChangeServiceMock.Implement("add_ComponentAdded",
                new object[] { MockConstraint.IsAnything<ComponentEventHandler>() });
            spMock.Fake_AddService(typeof(IComponentChangeService), componentChangeServiceMock.Instance);
            Mock<IVsHierarchy> hierarchyMock = new Mock<IVsHierarchy>(typeof(IVsProject));
            hierarchyMock.Implement(new MethodId(typeof(IVsProject), "GetItemContext"),
                new object[] { (uint)VSITEMID.ROOT, MockConstraint.IsAnything<Microsoft.VisualStudio.OLE.Interop.IServiceProvider>() },
                new object[] { (uint)0, null },
                VSConstants.E_FAIL);
            spMock.Fake_AddService(typeof(IVsHierarchy), hierarchyMock.Instance);

            ResourceEditorRootComponent rootComponent = new ResourceEditorRootComponent();

            Mock<ResourceFile> resourceFileMock = new Mock<ResourceFile>();
            resourceFileMock.SetCreateArguments(new object[] { null, rootComponent, spMock.Instance, basePath });
            SequenceMock<ResourceEditorDesignerLoader> designerLoaderMock = new SequenceMock<ResourceEditorDesignerLoader>();
            ResourceEditorViewMock view = new ResourceEditorViewMock(spMock.Instance);
            view.Fake_designerLoader = designerLoaderMock.Instance;
            resourceFileMock.Implement("get_View", view);
            Dictionary<string, object> styles = new Dictionary<string, object>();
            //Make The RunSingleFileGenerator call throw an exception, and make sure we don't
            //  blow up because of it.
            designerLoaderMock.AddExpectation("RunSingleFileGenerator",
                new object[] { true },
                new Exception("Whoops"));

            //Run test
            Microsoft_VisualStudio_Editors_ResourceEditor_ResourceFileAccessor accessor = new Microsoft_VisualStudio_Editors_ResourceEditor_ResourceFileAccessor(resourceFileMock.Instance);
            accessor.DelayFlushAndRunCustomToolImpl();

            //Verify
            Assert.AreEqual("Whoops", view.FakeResult_DSMsgBoxWasCalledWithThisString);
            designerLoaderMock.Verify();
        }

    }


    #region "ResourceEditorView subclass mocks"
    //Can't mock this class currently due to a mock framework bug (fixed now, but not yet integrated).

    class ResourceEditorViewMock : ResourceEditorView
    {
        //public ResourceEditorRootDesigner Fake_rootDesigner = null;
        public ResourceEditorDesignerLoader Fake_designerLoader = null;
        public DialogResult Fake_DsMsgReturnValue = DialogResult.OK;
        public string FakeResult_DSMsgBoxWasCalledWithThisString = null;

        public ResourceEditorViewMock(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        internal override ResourceEditorDesignerLoader GetDesignerLoader()
        {
            return Fake_designerLoader;
        }

        /*
        internal override ResourceEditorRootDesigner RootDesigner
        {
            get
            {
                return Fake_rootDesigner;
            }
        }
         */

        public override DialogResult DsMsgBox(string Message, System.Windows.Forms.MessageBoxButtons Buttons, System.Windows.Forms.MessageBoxIcon Icon, System.Windows.Forms.MessageBoxDefaultButton DefaultButton, string HelpLink)
        {
            FakeResult_DSMsgBoxWasCalledWithThisString = Message;
            return Fake_DsMsgReturnValue;
        }

        public override void DsMsgBox(Exception ex)
        {
            FakeResult_DSMsgBoxWasCalledWithThisString = ex.Message;
        }
    }

    #endregion

}
