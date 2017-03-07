using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using Microsoft.VisualStudio.Editors.PropertyPages.WPF;

namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class AppDotXamlDocumentTests
    {
        private const string EXCEPTION_COULDNTFINDAPPLICATION = "Could not find the expected root element \"Application\" in the application definition file.";

        public AppDotXamlDocumentTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

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

        #region Utilities

        private XmlTextReader CreateXmlReader(params string[] textLines)
        {
            string text = string.Join("\r\n", textLines);
            TextReader textReader = new StringReader(text);
            return new XmlTextReader(textReader);
        }

        /// <summary>
        /// Given a string, and a character such as #, determines the 0-based index of
        ///   the first instance of that character, and removes it from the string.
        ///   
        /// Example:
        ///   Input:  This is a #test#.
        ///   Output text: This is a text#.
        ///   Output codedCharIndex: 10
        /// </summary>
        /// <param name="text"></param>
        /// <param name="codedChar"></param>
        /// <param name="codedCharIndex"></param>
        /// <returns></returns>
        private string RemoveCodedCharacter(string text, char codedChar, out int codedCharIndex)
        {
            codedCharIndex = text.IndexOf(codedChar);
            if (codedCharIndex >= 0)
            {
                return text.Substring(0, codedCharIndex) + text.Substring(codedCharIndex + 1);
            }
            else
            {
                return text;
            }
        }

        private XmlTextReader CreateXmlReaderFromCodedText(out int startIndex, out int endIndex, out string text, params string[] textLines)
        {
            #region Test utilities
            {
                string testReturn;
                int testIndex;
                testReturn = RemoveCodedCharacter("#hello#", '#', out testIndex);
                Debug.Assert(testReturn.Equals("hello#") && testIndex == 0);

                testReturn = RemoveCodedCharacter("hello#", '#', out testIndex);
                Debug.Assert(testReturn.Equals("hello") && testIndex == 5);

                testReturn = RemoveCodedCharacter("hello", '#', out testIndex);
                Debug.Assert(testReturn.Equals("hello") && testIndex == -1);
            }
            #endregion

            text = string.Join("\r\n", textLines);
            text = RemoveCodedCharacter(text, '[', out startIndex);
            text = RemoveCodedCharacter(text, ']', out endIndex);

            return CreateXmlReader(text);
        }

        #endregion

        #region MoveToApplicationRootElement

        [TestMethod]
        public void MoveToApplicationRootElement()
        {
            XmlTextReader reader = CreateXmlReader(
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' StartupUri='MainWindow.xaml' ShutdownMode='OnExplicitShutdown'/>");
            AppDotXamlDocument.MoveToApplicationRootElement(reader);

            Assert.AreEqual("Application", reader.Name);
        }

        [TestMethod]
        public void MoveToApplicationRootElement_UnrecognizedRoot()
        {
            try
            {
                XmlTextReader reader = CreateXmlReader(
                    @"<NotApplication xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' StartupUri='MainWindow.xaml' ShutdownMode='OnExplicitShutdown'/>");
                AppDotXamlDocument.MoveToApplicationRootElement(reader);
                Assert.Fail("Expected exception");
            }
            catch (XamlReadWriteException ex)
            {
                Assert.AreEqual(EXCEPTION_COULDNTFINDAPPLICATION, ex.Message);
            }
        }

        [TestMethod]
        public void MoveToApplicationRootElement_UnrecognizedNested()
        {
            try
            {
                XmlTextReader reader = CreateXmlReader(
                    @"<NotApplication xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'> "
                    + @"  <Application/>"
                    + @"<NotApplication/>");
                AppDotXamlDocument.MoveToApplicationRootElement(reader);
                Assert.Fail("Expected exception");
            }
            catch (XamlReadWriteException ex)
            {
                Assert.AreEqual(EXCEPTION_COULDNTFINDAPPLICATION, ex.Message);
            }
        }

        [TestMethod]
        public void MoveToApplicationRootElement_Comments()
        {
            XmlTextReader reader = CreateXmlReader(
                @"<!-- This comment should be ignored --> <!-- and this... --> <Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' StartupUri='MainWindow.xaml' ShutdownMode='OnExplicitShutdown'/>");
            AppDotXamlDocument.MoveToApplicationRootElement(reader);

            Assert.AreEqual("Application", reader.Name);
        }

        #endregion

        #region FindApplicationPropertyInXaml

        [TestMethod]
        public void FindApplicationPropertyInXaml_SingleQuote()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<!-- This comment should be ignored --> <!-- and this... --> <Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' "
                    + "StartupUri  =   ['Main&amp;Window.xaml'] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("'Main&amp;Window.xaml'", property.ActualDefinitionText);
                    Assert.AreEqual("Main&Window.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_NoWhitespace()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<!-- This comment should be ignored --> <!-- and this... --> <Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' "
                    + "StartupUri=['Main&amp;Window.xaml'] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("'Main&amp;Window.xaml'", property.ActualDefinitionText);
                    Assert.AreEqual("Main&Window.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_LastAttribute()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<!-- This comment should be ignored --> <!-- and this... --> <Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' "
                    + "ShutdownMode='OnExplicitShutdown' StartupUri=['Main&amp;Window.xaml']/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("'Main&amp;Window.xaml'", property.ActualDefinitionText);
                    Assert.AreEqual("Main&Window.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_OnlyAttribute()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<!-- This comment should be ignored --> <!-- and this... --> <Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' "
                    + "StartupUri=['Main&amp;Window.xaml']/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("'Main&amp;Window.xaml'", property.ActualDefinitionText);
                    Assert.AreEqual("Main&Window.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_FollowedByChild()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<!-- This comment should be ignored --> <!-- and this... --> <Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' "
                    + "StartupUri=['Main&amp;Window.xaml']><Child></Child></Application>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("'Main&amp;Window.xaml'", property.ActualDefinitionText);
                    Assert.AreEqual("Main&Window.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_DoubleQuote()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<!-- This comment should be ignored --> <!-- and this... --> <Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' "
                    + "StartupUri  =   [\"Main&amp;Window.xaml\"] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("\"Main&amp;Window.xaml\"", property.ActualDefinitionText);
                    Assert.AreEqual("Main&Window.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_Multiline()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<!-- This comment should be ignored --> "
                + @"<!-- and this... --> "
                + @"<Application "
                + @"    xmlns='http://schemas.microsoft.com/presentation'"
                + @" xmlns:x='http://schemas.microsoft.com/xaml' "
                + @"StartupUri  "
                + "\r\n"
                + "  \r\n"
                + "\t  \r\n"
                + "  \t   = \t  \r\n"
                + " \t\t   \r\n"
                + "   \t\t\t  [\"Main&amp;Window.xaml\"] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("\"Main&amp;Window.xaml\"", property.ActualDefinitionText);
                    Assert.AreEqual("Main&Window.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_Multiline2()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<!-- This comment should be ignored --> ",
                @"<!-- and this... --> ",
                @"<Application ",
                @"    xmlns='http://schemas.microsoft.com/presentation'",
                @" xmlns:x='http://schemas.microsoft.com/xaml' ",
                @"StartupUri",
                "",
                "  ",
                "\t  ",
                "=",
                " \t\t   ",
                "[\"Main&amp;Window.xaml\"] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("\"Main&amp;Window.xaml\"", property.ActualDefinitionText);
                    Assert.AreEqual("Main&Window.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_Multiline3()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<!-- This comment should be ignored --> <!-- and this... --> <Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' StartupUri  ",
                "  ",
                "\t  ",
                "  \t   = \t  ",
                " \t\t   ",
                "   \t\t\t  [\"Main&amp;Window.xaml\"] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("\"Main&amp;Window.xaml\"", property.ActualDefinitionText);
                    Assert.AreEqual("Main&Window.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_WithMainWindow()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                "<Application ",
                "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                "StartupUri=[\"StartupWindow.xaml\"] >",
                "<Application.MainWindow>",
                "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                "</Application>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("\"StartupWindow.xaml\"", property.ActualDefinitionText);
                    Assert.AreEqual("StartupWindow.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(XmlException))]
        public void FindApplicationPropertyInXaml_ExpectedQuotes()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                "<Application ",
                "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                "StartupUri=StartupWindow.xaml >",
                "<Application.MainWindow>",
                "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                "</Application>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(XmlException))]
        public void FindApplicationPropertyInXaml_ExpectedEndQuote()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                "<Application ",
                "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                "StartupUri='StartupWindow.xaml >",
                "<Application.MainWindow>",
                "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                "</Application>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_ContainsSingleQuote()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'",
                "StartupUri=[\"'MainWindow'.xaml\"] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("\"'MainWindow'.xaml\"", property.ActualDefinitionText);
                    Assert.AreEqual("'MainWindow'.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_ContainsSingleEscaped()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'",
                "StartupUri=[\"&apos;MainWindow&apos;.xaml\"] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("\"&apos;MainWindow&apos;.xaml\"", property.ActualDefinitionText);
                    Assert.AreEqual("'MainWindow'.xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_ContainsDoubleQuote()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'",
                "StartupUri=['\"MainWindow\".xaml'] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("'\"MainWindow\".xaml'", property.ActualDefinitionText);
                    Assert.AreEqual("\"MainWindow\".xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_ContainsDoubleQuoteEscaped()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'",
                "StartupUri=[\"&quot;MainWindow&quot;.xaml\"] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("\"&quot;MainWindow&quot;.xaml\"", property.ActualDefinitionText);
                    Assert.AreEqual("\"MainWindow\".xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_ContainsEquals()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'",
                "StartupUri=[\"Main=Window.xaml\"] ShutdownMode='OnExplicitShutdown'/>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("\"Main=Window.xaml\"", property.ActualDefinitionText);
                    Assert.AreEqual("Main=Window.xaml", property.UnescapedValue);
                }
            }
        }

        #endregion

        #region FindApplicationPropertyInXaml - property element syntax

        [TestMethod]
        public void FindApplicationPropertyInXaml_PropertyElementSyntax()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'>",
                "  <Application.StartupUri>Main&lt;'s \"Window\".xaml</Application.StartupUri>",
                "</Application>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("Main&lt;'s \"Window\".xaml", property.ActualDefinitionText);
                    Assert.AreEqual("Main<'s \"Window\".xaml", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_PropertyElementSyntax_Empty()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'>",
                "  <Application.StartupUri></Application.StartupUri>",
                "</Application>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("", property.ActualDefinitionText);
                    Assert.AreEqual("", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_PropertyElementSyntax_Whitespace()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'>",
                "  <Application.StartupUri>  </Application.StartupUri>",
                "</Application>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("  ", property.ActualDefinitionText);
                    Assert.AreEqual("", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_PropertyElementSyntax_Whitespace2()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'>",
                "  <Application.StartupUri>  foo ",
                "  </Application.StartupUri>",
                "</Application>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("  foo \r\n  ", property.ActualDefinitionText);
                    Assert.AreEqual("foo", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_PropertyElementSyntax_EmptyTag()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'>",
                "  <Application.StartupUri/>",
                "</Application>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("", property.ActualDefinitionText);
                    Assert.AreEqual("", property.UnescapedValue);
                }
            }
        }

        [TestMethod]
        public void FindApplicationPropertyInXaml_PropertyElementSyntax_Multiline()
        {
            int startIndex, endIndex;
            string text;
            XmlTextReader xmlReader = CreateXmlReaderFromCodedText(
                out startIndex, out endIndex, out text,
                @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'>",
                "  <Application.StartupUri>[\"Main&lt;Window.xaml\"]",
                "  </Application.StartupUri>",
                "</Application>");
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    AppDotXamlDocument.XamlProperty property = xamlReader.FindApplicationPropertyInXaml(xmlReader, "StartupUri");
                    Assert.IsNotNull(property);
                    Assert.AreEqual("\"Main&lt;Window.xaml\"\r\n  ", property.ActualDefinitionText);
                    Assert.AreEqual("\"Main<Window.xaml\"", property.UnescapedValue);
                    Assert.AreEqual(1, property.DefinitionStart.LineIndex);
                    Assert.AreEqual(26, property.DefinitionStart.CharIndex);
                    Assert.AreEqual(2, property.DefinitionEndPlusOne.LineIndex);
                    Assert.AreEqual(2, property.DefinitionEndPlusOne.CharIndex);
                }
            }
        }

        #endregion

        #region EscapeXmlString

        [TestMethod]
        public void EscapeXmlString_Null()
        {
            Assert.AreEqual("", AppDotXamlDocument.EscapeXmlString(null));
        }

        [TestMethod]
        public void EscapeXmlString_EmptyString()
        {
            Assert.AreEqual("", AppDotXamlDocument.EscapeXmlString(""));
        }

        [TestMethod]
        public void EscapeXmlString_Normal()
        {
            Assert.AreEqual("abc.def", AppDotXamlDocument.EscapeXmlString("abc.def"));
        }

        [TestMethod]
        public void EscapeXmlString_MiscChars()
        {
            Assert.AreEqual("lt&lt;, gr&gt;, squote&apos;, dquote&quot;, equals=, tab\t, esszetß",
                AppDotXamlDocument.EscapeXmlString("lt<, gr>, squote', dquote\", equals=, tab\t, esszetß"));
        }

        #endregion

        #region UnescapeXmlContent

        [TestMethod]
        public void UnescapeXmlContent_Null()
        {
            Assert.AreEqual("", AppDotXamlDocument.UnescapeXmlContent(null));
        }

        [TestMethod]
        public void UnescapeXmlContent_EmptyString()
        {
            Assert.AreEqual("", AppDotXamlDocument.UnescapeXmlContent(""));
        }

        [TestMethod]
        public void UnescapeXmlContent_Normal()
        {
            Assert.AreEqual("abc.def", AppDotXamlDocument.UnescapeXmlContent("abc.def"));
        }

        [TestMethod]
        public void UnescapeXmlContent_MiscChars()
        {
            Assert.AreEqual("lt<, gr>, squote', dquote\", equals=, tab\t, esszetß",
                AppDotXamlDocument.UnescapeXmlContent("lt&lt;, gr&gt;, squote&apos;, dquote&quot;, equals=, tab\t, esszetß"));
        }

        [TestMethod]
        public void UnescapeXmlContent_CDATA()
        {
            Assert.AreEqual("'Räksmörgås' \"hi\".xaml",
                AppDotXamlDocument.UnescapeXmlContent("'Räk<![CDATA[smörg]]>ås' \"hi&quot;.xaml"));
        }

        #endregion

        #region FindClosingAngleBracket

        private void Test_FindClosingAngleBracket(string text, int iStartLine, int iStartIndex, int iExpectedLine, int iExpectedIndex)
        {
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument reader = new AppDotXamlDocument(buffer))
                {
                    AppDotXamlDocument.Location location = reader.FindClosingAngleBracket(
                        new AppDotXamlDocument.Location(iStartLine, iStartIndex));

                    if (iExpectedLine == -1)
                    {
                        Assert.IsNull(location,
                            "Expected to not find the closing angle bracket, but we returned a non-null location");

                    }
                    else
                    {
                        Assert.IsNotNull(location, "Didn't find the closing angle bracket");
                        Assert.AreEqual(iExpectedLine, location.LineIndex,
                            "Didn't find the closing angle bracket on the expected line");
                        Assert.AreEqual(iExpectedIndex, location.CharIndex,
                            "Didn't find the closing angle bracket at the expected character index");
                    }
                }
            }
        }
        [TestMethod]
        public void FindClosingAngleBracket_Simple()
        {
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "<Application>", 
                }),
                0, 1,
                0, 12
                );
        }


        [TestMethod]
        public void FindClosingAngleBracket_SimpleMultiline()
        {
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "<Application>", 
                    "",
                }),
                0, 0,
                1, 12
                );
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "",
                    "<Application>", 
                    "",
                }),
                0, 0,
                2, 12
                );
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "",
                    "<Application>", 
                    "",
                }),
                1, 0,
                2, 12
                );
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "",
                    "<Application>", 
                    "",
                }),
                2, 0,
                2, 12
                );
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "",
                    "<Application>", 
                    "",
                }),
                2, 12,
                2, 12
                );
        }

        [TestMethod]
        public void FindClosingAngleBracket_SimpleMultilineNotFound()
        {
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "",
                    "<Application>", 
                    "",
                }),
                2, 13,
                -1, -1
                );
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "",
                    "<Application>", 
                    "",
                }),
                3, 0,
                -1, -1
                );
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "",
                    "<Application///////", 
                    "",
                }),
                0, 0,
                -1, -1
                );
        }

        [TestMethod]
        public void FindClosingAngleBracket_WithSlash()
        {
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "",
                    "<Application/>>>>", 
                    "",
                }),
                0, 0,
                2, 12
                );
        }

        [TestMethod]
        public void FindClosingAngleBracket_SkipStrings()
        {
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "",
                    "<Application foo='/>>>>' bar=\">>/>>/>>\"/>  <foo>",
                    "",
                }),
                0, 0,
                2, 39
                );
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "",
                    "",
                    "<Application foo=\"/>'>>\" bar='>\"/>>/>>'>  <foobar/>",
                    "",
                }),
                0, 0,
                2, 39
                );
        }

        [TestMethod]
        public void FindClosingAngleBracket_SkipStrings2()
        {
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "foo",
                    "'bar' 'goose'",
                    "<Application foo='/>>>>' bar=\">>/>>/>>\"/",
                    "/>",
                }),
                0, 0,
                3, 0
                );
        }

        [TestMethod]
        public void FindClosingAngleBracket_NonWellformedStrings()
        {
            Test_FindClosingAngleBracket(
                string.Join("\r\n", new string[] {
                    "'foo",
                    "\"bar\" and \"goose",
                    "<Application foo='/>>>>' bar=\">>/>>/>>\"/>  <foo>",
                    "",
                }),
                0, 0,
                2, 39
                );
        }

        #endregion

        #region GetStartupUri

        [TestMethod]
        public void GetStartupUri()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("StartupWindow.xaml", value);
                }
            }
        }

        [TestMethod]
        public void GetStartupUri_NotFound()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUriNotFound=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("", value);
                }
            }
        }

        #endregion

        #region SetStartupUri

        [TestMethod]
        public void SetStartupUri()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();
                    Assert.AreEqual("StartupWindow.xaml", value);

                    xamlReader.SetStartupUri("NewStartup.xaml");

                    Assert.AreEqual("NewStartup.xaml", xamlReader.GetStartupUri());
                }
            }
        }

        [TestMethod]
        public void SetStartupUri2()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application",
                    "    x:Class=\"SDKSamples.MyApp\"",
                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "    StartupUri=\"Page1.xaml\"",
                    "    >",
                    "",
                    "  <!-- Global style definitions and other resources go here. -->",
                    "  <Application.Resources>",
                    "    <Style TargetType=\"{x:Type Label}\">",
                    "      <Setter Property=\"Background\">",
                    "        <Setter.Value>",
                    "          <LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"0,1\">",
                    "            <LinearGradientBrush.GradientStops>",
                    "              <GradientStop Offset=\"0\" Color=\"White\" />",
                    "              <GradientStop Offset=\"1\" Color=\"LightSteelBlue\" />",
                    "            </LinearGradientBrush.GradientStops>",
                    "          </LinearGradientBrush>",
                    "        </Setter.Value>",
                    "      </Setter>",
                    "    </Style>",
                    "  </Application.Resources>",
                    "",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();
                    Assert.AreEqual("Page1.xaml", value);

                    xamlReader.SetStartupUri("NewStartup.xaml");

                    Assert.AreEqual("NewStartup.xaml", xamlReader.GetStartupUri());
                }
            }
        }

        [TestMethod]
        public void SetStartupUri_FullyQualified()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "Application.StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();
                    Assert.AreEqual("StartupWindow.xaml", value);

                    xamlReader.SetStartupUri("NewStartup.xaml");

                    Assert.AreEqual("NewStartup.xaml", xamlReader.GetStartupUri());
                }
            }
        }

        [TestMethod]
        public void SetStartupUri_EmptyNull()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();
                    Assert.AreEqual("StartupWindow.xaml", value);

                    xamlReader.SetStartupUri(null);
                    Assert.AreEqual("", xamlReader.GetStartupUri());

                    xamlReader.SetStartupUri("");
                    Assert.AreEqual("", xamlReader.GetStartupUri());
                }
            }
        }

        [TestMethod]
        public void SetStartupUri_FormattingNotChanged1()
        {
            string text = "<Application StartupUri=\"StartupWindow.xaml\" />";
            string expectedText = "<Application StartupUri=\"NewStartup.xaml\" />";
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();
                    Assert.AreEqual("StartupWindow.xaml", value);

                    xamlReader.SetStartupUri("NewStartup.xaml");

                    Assert.AreEqual("NewStartup.xaml", xamlReader.GetStartupUri());
                    Assert.AreEqual(expectedText, buffer.Fake_AllText);
                }
            }
        }

        [TestMethod]
        public void SetStartupUri_FormattingNotChanged2()
        {
            string text = "<Application StartupUri  =  \"StartupWindow.xaml\"/>";
            string expectedText = "<Application StartupUri  =  \"NewStartup.xaml\"/>";
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();
                    Assert.AreEqual("StartupWindow.xaml", value);

                    xamlReader.SetStartupUri("NewStartup.xaml");

                    Assert.AreEqual("NewStartup.xaml", xamlReader.GetStartupUri());
                    Assert.AreEqual(expectedText, buffer.Fake_AllText);
                }
            }
        }

        [TestMethod]
        public void SetStartupUri_FormattingNotChanged3()
        {
            string text = "<Application StartupUri  \r\n= \r\n \"StartupWindow.xaml\"/>";
            string expectedText = "<Application StartupUri  \r\n= \r\n \"NewStartup.xaml\"/>";
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();
                    Assert.AreEqual("StartupWindow.xaml", value);

                    xamlReader.SetStartupUri("NewStartup.xaml");

                    Assert.AreEqual("NewStartup.xaml", xamlReader.GetStartupUri());
                    Assert.AreEqual(expectedText, buffer.Fake_AllText);
                }
            }
        }

        [TestMethod]
        public void SetStartupUri_SingleQuotes()
        {
            string text = "<Application StartupUri  =  'StartupWindow.xaml'/>";
            string expectedText = "<Application StartupUri  =  \"NewStartup.xaml\"/>";
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();
                    Assert.AreEqual("StartupWindow.xaml", value);

                    xamlReader.SetStartupUri("NewStartup.xaml");

                    Assert.AreEqual("NewStartup.xaml", xamlReader.GetStartupUri());
                    Assert.AreEqual(expectedText, buffer.Fake_AllText);
                }
            }
        }

        [TestMethod]
        public void SetStartupUri_EscapedCharacters()
        {
            string text = "<Application StartupUri  =  '&quot;Startup&amp;Window&quot;.xaml'/>";
            string expectedText = "<Application StartupUri  =  \"&quot;New&quot;Start&amp;up.xaml\"/>";
            string newText = "\"New\"Start&up.xaml";
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();
                    Assert.AreEqual("\"Startup&Window\".xaml", value);

                    xamlReader.SetStartupUri(newText);

                    Assert.AreEqual(newText, xamlReader.GetStartupUri());
                    Assert.AreEqual(expectedText, buffer.Fake_AllText);
                }
            }
        }

        private void Test_SetStartupUri(string text, string expectedOldValue, string newValue, string expectedText)
        {
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();
                    Assert.AreEqual(expectedOldValue, value);

                    xamlReader.SetStartupUri(newValue);

                    Assert.AreEqual(newValue, xamlReader.GetStartupUri());
                    Assert.AreEqual(expectedText, buffer.Fake_AllText);
                }
            }
        }

        private void Test_SetStartupUri_Missing(string text, string newValue, string expectedText)
        {
            Test_SetStartupUri(text, "", newValue, expectedText);
        }

        [TestMethod]
        public void SetStartupUri_Missing()
        {
            Test_SetStartupUri_Missing(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "foo='blah'",
                    ">",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "NewStartup.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "foo='blah'",
                    "StartupUri=\"NewStartup.xaml\">",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
                    );
        }

        [TestMethod]
        public void SetStartupUri_Missing2()
        {
            Test_SetStartupUri_Missing(
                string.Join("\r\n", new string[] {
                    "<Application",
                    "    x:Class=\"SDKSamples.MyApp\"",
                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "    >",
                    "",
                    "  <!-- Global style definitions and other resources go here. -->",
                    "  <Application.Resources>",
                    "    <Style TargetType=\"{x:Type Label}\">",
                    "      <Setter Property=\"Background\">",
                    "        <Setter.Value>",
                    "          <LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"0,1\">",
                    "            <LinearGradientBrush.GradientStops>",
                    "              <GradientStop Offset=\"0\" Color=\"White\" />",
                    "              <GradientStop Offset=\"1\" Color=\"LightSteelBlue\" />",
                    "            </LinearGradientBrush.GradientStops>",
                    "          </LinearGradientBrush>",
                    "        </Setter.Value>",
                    "      </Setter>",
                    "    </Style>",
                    "  </Application.Resources>",
                    "",
                    "</Application>"}),
                "NewStartup.xaml",
                string.Join("\r\n", new string[] {
                    "<Application",
                    "    x:Class=\"SDKSamples.MyApp\"",
                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "    StartupUri=\"NewStartup.xaml\">",
                    "",
                    "  <!-- Global style definitions and other resources go here. -->",
                    "  <Application.Resources>",
                    "    <Style TargetType=\"{x:Type Label}\">",
                    "      <Setter Property=\"Background\">",
                    "        <Setter.Value>",
                    "          <LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"0,1\">",
                    "            <LinearGradientBrush.GradientStops>",
                    "              <GradientStop Offset=\"0\" Color=\"White\" />",
                    "              <GradientStop Offset=\"1\" Color=\"LightSteelBlue\" />",
                    "            </LinearGradientBrush.GradientStops>",
                    "          </LinearGradientBrush>",
                    "        </Setter.Value>",
                    "      </Setter>",
                    "    </Style>",
                    "  </Application.Resources>",
                    "",
                    "</Application>"})
                    );
        }

        [TestMethod]
        public void SetStartupUri_Missing_Empty_NothingChanges()
        {
            Test_SetStartupUri_Missing(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "foo='blah'",
                    ">",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "foo='blah'",
                    ">",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetStartupUri_Missing_NeedsCRLF()
        {
            Test_SetStartupUri_Missing(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "foo='blah'>",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "NewStartup.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "foo='blah'",
                    "StartupUri=\"NewStartup.xaml\">",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
                    );
        }

        [TestMethod]
        public void SetStartupUri_Missing_NoClosingTag()
        {
            Test_SetStartupUri_Missing(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "/>"}),
                "NewStartup.xaml",
               string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"NewStartup.xaml\"/>"})
            );
        }


        [TestMethod]
        public void SetStartupUri_Missing_NoClosingTag_NeedsCRLF()
        {
            Test_SetStartupUri_Missing(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"/>"}),
                "NewStartup.xaml",
               string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"NewStartup.xaml\"/>"})
            );
        }

        [TestMethod]
        public void SetStartupUri_Missing_WithClosingTagButNoChildren()
        {
            Test_SetStartupUri_Missing(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "></Application>"}),
                "NewStartup.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"NewStartup.xaml\"></Application>"})
            );
        }

        [TestMethod]
        public void SetStartupUri_Missing_WithChildren()
        {
            Test_SetStartupUri_Missing(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"><Child1></Child1></Application>"}),
                "NewStartup.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"NewStartup.xaml\"><Child1></Child1></Application>"})
            );
        }

        [TestMethod]
        [ExpectedException(typeof(XmlException))]
        public void SetStartupUri_Missing_MissingClosingTag()
        {
            Test_SetStartupUri_Missing(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    ""}),
                "NewStartup.xaml",
                ""
            );
        }

        [TestMethod]
        [ExpectedException(typeof(XmlException))]
        [Ignore] // Throws assertion, but only in debug verification code.  Can ignore this test for now.
        public void SetStartupUri_Missing_MissingClosingTag2()
        {
            Test_SetStartupUri_Missing(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    ">"}),
                "NewStartup.xaml",
                ""
            );
        }

        #endregion

        #region GetStartupUri - Property element syntax

        [TestMethod]
        public void GetStartupUri_PropertyElementSyntax()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>Startup&amp;Window.xaml</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("Startup&Window.xaml", value);
                }
            }
        }
        
        [TestMethod]
        public void GetStartupUri_PropertyElementSyntax_CDATA1()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>Räk<![CDATA[smörg]]>ås.xaml</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("Räksmörgås.xaml", value);
                }
            }
        }

        [TestMethod]
        public void GetStartupUri_PropertyElementSyntax_CDATA2()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri><![CDATA[smörg]]></Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("smörg", value);
                }
            }
        }

        [TestMethod]
        public void GetStartupUri_PropertyElementSyntax_CDATA3()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>",
                    "<![CDATA[smörg]]></Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("smörg", value);
                }
            }
        }

        [TestMethod]
        public void GetStartupUri_PropertyElementSyntax_CDATA4()
        {
            string text = string.Join("\r\n", new string[]{
                @"<Application>",
                "  <Application.StartupUri>Räk<![CDATA[smörg]]>ås.xaml</Application.StartupUri>",
                "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("Räksmörgås.xaml", value);
                }
            }
        }

        [TestMethod]
        public void GetStartupUri_PropertyElementSyntax_CDATA5()
        {
            string text = string.Join("\r\n", new string[]{
                    "<?xml version=\"1.0\" encoding=\"x-cp20261\"?>",
                    "<Application x:Class=\"Application\"",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    ">",
                    "<Application.StartupUri>Räk<![CDATA[smörg]]>ås.xaml</Application.StartupUri>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("Räksmörgås.xaml", value);
                }
            }
        }

        [TestMethod]
        public void GetStartupUri_PropertyElementSyntax_Empty()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>Startup&amp;Window.xaml</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("Startup&Window.xaml", value);
                }
            }
        }

        [TestMethod]
        public void GetStartupUri_PropertyElementSyntax_EmptyTag()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri/>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("", value);
                }
            }
        }

        [TestMethod]
        public void GetStartupUri_PropertyElementSyntax_NotRecognizedIfNotFullyQualified()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <StartupUri>Startup&amp;Window.xaml</StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"});
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetStartupUri();

                    Assert.AreEqual("", value);
                }
            }
        }

        #endregion

        #region SetStartupUri - Property element syntax

        [TestMethod]
        public void SetStartupUri_PropertyElementSyntax()
        {
            Test_SetStartupUri(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>Startup&amp;Window.xaml</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "Startup&Window.xaml",
                "New\"Start'up\".xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>New&quot;Start&apos;up&quot;.xaml</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetStartupUri_PropertyElementSyntax_Empty()
        {
            Test_SetStartupUri(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri></Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "",
                "New\"Start'up\".xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>New&quot;Start&apos;up&quot;.xaml</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetStartupUri_PropertyElementSyntax_Empty2()
        {
            Test_SetStartupUri(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>",
                    "  </Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "",
                "New\"Start'up\".xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>New&quot;Start&apos;up&quot;.xaml</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetStartupUri_PropertyElementSyntax_EmptyTag()
        {
            Test_SetStartupUri(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri/>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "",
                "New\"Start'up\".xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>New&quot;Start&apos;up&quot;.xaml</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        [ExpectedException(typeof(XmlException))]
        public void SetStartupUri_PropertyElementSyntax_EmptyTag2()
        {
            Test_SetStartupUri(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <  StartupUri   />",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "",
                "New\"Start'up\".xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>New&quot;Start&apos;up&quot;.xaml</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetStartupUri_PropertyElementSyntax_Missing()
        {
            Test_SetStartupUri(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "",
                "New\"Start'up\".xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"New&quot;Start&apos;up&quot;.xaml\">",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetStartupUri_PropertyElementSyntax_Missing_ApplicationEmpty1()
        {
            Test_SetStartupUri(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "</Application>"}),
                "",
                "New\"Start'up\".xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"New&quot;Start&apos;up&quot;.xaml\">",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetStartupUri_PropertyElementSyntax_Missing_ApplicationEmpty2()
        {
            Test_SetStartupUri(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"/>"}),
                "",
                "New\"Start'up\".xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"New&quot;Start&apos;up&quot;.xaml\"/>"})
            );
        }


        [TestMethod]
        public void SetStartupUri_PropertyElementSyntax_CDATA()
        {
            Test_SetStartupUri(
                string.Join("\r\n", new string[] {
                    @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'>",
                    "  <Application.StartupUri>Räk<![CDATA[smörg]]>ås.xaml</Application.StartupUri>",
                    "</Application>"}),
                "Räksmörgås.xaml",
                "What's a Räksmörgås, anyway?.xaml",
                string.Join("\r\n", new string[] {
                    "<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml'>",
                    "  <Application.StartupUri>What&apos;s a Räksmörgås, anyway?.xaml</Application.StartupUri>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetStartupUri_PropertyElementSyntax_Multiline()
        {
            Test_SetStartupUri(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>",
                    "    Startup&amp;Window.xaml    ",
                    "",
                    "</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "Startup&Window.xaml",
                "New\"Start'up\".xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">",
                    "  <Application.StartupUri>New&quot;Start&apos;up&quot;.xaml</Application.StartupUri>",
                    "  <Application.MainWindow>",
                    "  <NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
            );
        }

        #endregion

        #region VerifyAppXamlIsValidAndThrowIfNot

        private void Test_CallVerifyAppXamlIsValidAndThrowIfNot(string text)
        {
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    xamlReader.VerifyAppXamlIsValidAndThrowIfNot();
                }
            }
        }

        [TestMethod]
        public void VerifyAppXamlIsValidAndThrowIfNot_Valid_NoException()
        {
            Test_CallVerifyAppXamlIsValidAndThrowIfNot(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}));

            Test_CallVerifyAppXamlIsValidAndThrowIfNot(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"\" >",
                    "StartupUriBad=\"\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}));

            Test_CallVerifyAppXamlIsValidAndThrowIfNot(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"\" >",
                    "StartupUriBad=\"\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}));

            Test_CallVerifyAppXamlIsValidAndThrowIfNot(
                string.Join("\r\n", new string[] {
                    "<!-- comments --> <Application/> "}));
        }

        [TestMethod]
        [ExpectedException(typeof(XamlReadWriteException))]
        public void VerifyAppXamlIsValidAndThrowIfNot_NoApplicationRoot()
        {
            Test_CallVerifyAppXamlIsValidAndThrowIfNot(
                string.Join("\r\n", new string[] {
                    "<Application2 ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}));
        }

        [TestMethod]
        [ExpectedException(typeof(XmlException))]
        public void VerifyAppXamlIsValidAndThrowIfNot_ApplicationRootNotClosed()
        {
            Test_CallVerifyAppXamlIsValidAndThrowIfNot(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    ""}));
        }

        [TestMethod]
        [ExpectedException(typeof(XmlException))]
        public void VerifyAppXamlIsValidAndThrowIfNot_BadSyntax()
        {
            Test_CallVerifyAppXamlIsValidAndThrowIfNot(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow/>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}));
        }

        #endregion

        #region GetShutdownMode

        [TestMethod]
        public void GetShutdownMode()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' ShutdownMode='onexplicitshutdown'><Child1></Child1></Application>"}
                    );
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetShutdownMode();

                    Assert.AreEqual("onexplicitshutdown", value);
                }
            }
        }

        [TestMethod]
        public void GetShutdownMode_FullyQualified()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' Application.ShutdownMode='onexplicitshutdown'><Child1></Child1></Application>"}
                    );
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetShutdownMode();

                    Assert.AreEqual("onexplicitshutdown", value);
                }
            }
        }

        [TestMethod]
        public void GetShutdownMode_WrongQualifier()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' Application2.ShutdownMode='onexplicitshutdown'><Child1></Child1></Application>"}
                    );
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetShutdownMode();

                    Assert.AreEqual("", value);
                }
            }
        }

        [TestMethod]
        public void GetShutdownModeInvalid()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' ShutdownMode='goo'><Child1></Child1></Application>"}
                    );
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetShutdownMode();

                    Assert.AreEqual("goo", value); //UNDONE:?
                }
            }
        }

        [TestMethod]
        public void GetShutdownModeMissing()
        {
            string text =
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' ShutdownMode=''><Child1></Child1></Application>"}
                    );
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {

                    string value = xamlReader.GetShutdownMode();

                    Assert.AreEqual("", value); //UNDONE: ?
                }
            }
        }



        #endregion

        #region SetShutdownMode

        private void Test_SetShutdownMode(string text, string expectedCurrentValue, string newText, string expectedText)
        {
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetShutdownMode();
                    Assert.AreEqual(expectedCurrentValue, value);

                    xamlReader.SetShutdownMode(newText);

                    Assert.AreEqual(newText, xamlReader.GetShutdownMode());
                    Assert.AreEqual(expectedText, buffer.Fake_AllText);
                }
            }
        }

        [TestMethod]
        public void SetShutdownMode()
        {
            Test_SetShutdownMode(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' ShutdownMode='onexplicitshutdown'><Child1></Child1></Application>"}),
                "onexplicitshutdown",
                "OnLastWindowClose",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' ShutdownMode=\"OnLastWindowClose\"><Child1></Child1></Application>"})
            );
        }

        [TestMethod]
        public void SetShutdownMode_FullyQualified()
        {
            Test_SetShutdownMode(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' Application.ShutdownMode='onexplicitshutdown'><Child1></Child1></Application>"}),
                "onexplicitshutdown",
                "OnLastWindowClose",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' Application.ShutdownMode=\"OnLastWindowClose\"><Child1></Child1></Application>"})
            );
        }

        [TestMethod]
        public void SetShutdownMode_FullyQualified_WrongQualifier()
        {
            Test_SetShutdownMode(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' Application2.ShutdownMode='onexplicitshutdown'><Child1></Child1></Application>"}),
                "",
                "OnLastWindowClose",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri='foo' Application2.ShutdownMode='onexplicitshutdown'",
                    "ShutdownMode=\"OnLastWindowClose\"><Child1></Child1></Application>"})
            );
        }

        [TestMethod]
        public void SetShutdownMode_Missing()
        {
            Test_SetShutdownMode(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"Startup.xaml\"><Child1></Child1></Application>"}),
                "", //UNDONE:?
                "OnMainWindowClose",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"Startup.xaml\"",
                    "ShutdownMode=\"OnMainWindowClose\"><Child1></Child1></Application>"})
            );
        }

        #endregion

        #region GetMainWindow (CUT from spec)
#if false
        private void Test_GetMainWindow(string text, string expectedValue)
        {
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    string value = xamlReader.GetMainWindow();
                    Assert.AreEqual(expectedValue, value);
                }
            }
        }

        [TestMethod]
        public void GetMainWindow()
        {
            Test_GetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "MainPage.xaml");
        }

        [TestMethod]
        public void GetMainWindow_ApplicationMainWindowMissing()
        {
            Test_GetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "</Application>"}),
                "");
        }

        [TestMethod]
        public void GetMainWindow_ApplicationMainWindowEmpty()
        {
            Test_GetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow/>",
                    "</Application>"}),
                "");
        }

        [TestMethod]
        public void GetMainWindow_NavigationWindowMissing()
        {
            Test_GetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "</Application.MainWindow>",
                    "</Application>"}),
                "");
        }

        [TestMethod]
        public void GetMainWindow_NavigationWindowEmpty()
        {
            Test_GetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow /> </Application.MainWindow>",
                    "</Application>"}),
                "");
        }

        [TestMethod]
        public void GetMainWindow_SourceMissing()
        {
            Test_GetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "");
        }

        [TestMethod]
        public void GetMainWindow_SourceExistsButVisibleMissing_Ok()
        {
            Test_GetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"Main&amp;Page.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "Main&Page.xaml");
        }

#endif
        #endregion

        #region SetMainWindow (CUT from the spec)
#if false

        private void Test_SetMainWindow(string text, string expectedOldValue, string newValue, string expectedNewText)
        {
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer)) {
                string value = xamlReader.GetMainWindow();
                Assert.AreEqual(expectedOldValue, value);

                xamlReader.SetMainWindow(newValue);

                Assert.AreEqual(newValue, xamlReader.GetMainWindow());
                Assert.AreEqual(expectedNewText, buffer.Fake_AllText);
        }
            }
        }


        [TestMethod]
        public void SetMainWindow()
        {
            Test_SetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "MainPage.xaml",
                "New&Page.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"New&amp;Page.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
                );
        }

        [TestMethod]
        public void SetMainWindow2()
        {
            Test_SetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application",
                    "    x:Class=\"SDKSamples.MyApp\"",
                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "    StartupUri=\"Page1.xaml\"",
                    "    >",
                    "",
                    "  <!-- Global style definitions and other resources go here. -->",
                    "  <Application.Resources>",
                    "    <Style TargetType=\"{x:Type Label}\">",
                    "      <Setter Property=\"Background\">",
                    "        <Setter.Value>",
                    "          <LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"0,1\">",
                    "            <LinearGradientBrush.GradientStops>",
                    "              <GradientStop Offset=\"0\" Color=\"White\" />",
                    "              <GradientStop Offset=\"1\" Color=\"LightSteelBlue\" />",
                    "            </LinearGradientBrush.GradientStops>",
                    "          </LinearGradientBrush>",
                    "        </Setter.Value>",
                    "      </Setter>",
                    "    </Style>",
                    "  </Application.Resources>",
                    "",
                    "</Application>"}),
                "",
                "New\"Page.xaml",
                string.Join("\r\n", new string[] {
                    "<Application",
                    "    x:Class=\"SDKSamples.MyApp\"",
                    "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"",
                    "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "    StartupUri=\"Page1.xaml\"",
                    "    >",
                    "",
                    "  <!-- Global style definitions and other resources go here. -->",
                    "  <Application.Resources>",
                    "    <Style TargetType=\"{x:Type Label}\">",
                    "      <Setter Property=\"Background\">",
                    "        <Setter.Value>",
                    "          <LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"0,1\">",
                    "            <LinearGradientBrush.GradientStops>",
                    "              <GradientStop Offset=\"0\" Color=\"White\" />",
                    "              <GradientStop Offset=\"1\" Color=\"LightSteelBlue\" />",
                    "            </LinearGradientBrush.GradientStops>",
                    "          </LinearGradientBrush>",
                    "        </Setter.Value>",
                    "      </Setter>",
                    "    </Style>",
                    "  </Application.Resources>",
                    "",
                    "</Application>"})
                );
        }

        [TestMethod]
        public void SetMainWindow_VisibleIsFalse()
        {
            Test_SetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"MainPage.xaml\" Visibility=\"Hidden\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "MainPage.xaml",
                "New&Page.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"New&amp;Page.xaml\" Visibility=\"Hidden\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
                );
        }

        [TestMethod]
        public void SetMainWindow_Application_Empty()
        {
            Test_SetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\"/>"}),
                "",
                "New&Page.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\">",
                    "<Application.MainWindow>",
                    "  <NavigationWindow Source=\"New&amp;Page.xaml\" Visibility=\"Visible\"",
                    "  </NavigationWindow>",
                    "</Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetMainWindow_ApplicationMainWindowMissing()
        {
            Test_SetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "</Application>"}),
                "",
                "New&Page.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "  <NavigationWindow Source=\"New&amp;Page.xaml\" Visibility=\"Visible\"",
                    "  </NavigationWindow>",
                    "</Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetMainWindow_ApplicationMainWindowEmpty()
        {
            Test_SetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow/>",
                    "</Application>"}),
                "",
                "New&Page.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "  <NavigationWindow Source=\"New&amp;Page.xaml\" Visibility=\"Visible\"",
                    "  </NavigationWindow>",
                    "</Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetMainWindow_NavigationWindowMissing()
        {
            Test_SetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "</Application.MainWindow>",
                    "</Application>"}),
                "",
                "New&Page.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                                    "<Application.MainWindow>",
                    "  <NavigationWindow Source=\"New&amp;Page.xaml\" Visibility=\"Visible\"",
                    "  </NavigationWindow>",
                    "</Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetMainWindow_NavigationWindowEmpty()
        {
            Test_SetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow /> </Application.MainWindow>",
                    "</Application>"}),
                "",
                "New&Page.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "  <NavigationWindow Source=\"New&amp;Page.xaml\" Visibility=\"Visible\"",
                    "  </NavigationWindow>",
                    "</Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetMainWindow_SourceMissing()
        {
            Test_SetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "",
                "New&Page.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Visibility=\"Visible\"></NavigationWindow>",
                    "Source=\"New&amp;Page.xaml\"</Application.MainWindow>",
                    "</Application>"})
            );
        }

        [TestMethod]
        public void SetMainWindow_SourceExistsButVisibleMissing_Ok()
        {
            Test_SetMainWindow(
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"Main&amp;Page.xaml\" Visibility=\"Visible\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"}),
                "Main&Page.xaml",
                "New&Page.xaml",
                string.Join("\r\n", new string[] {
                    "<Application ",
                    "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"",
                    "StartupUri=\"StartupWindow.xaml\" >",
                    "<Application.MainWindow>",
                    "<NavigationWindow Source=\"New&amp;Page.xaml\"></NavigationWindow> </Application.MainWindow>",
                    "</Application>"})
            );
        }

#endif
        #endregion

        #region MakeSureElementHasStartAndEndTags

        private void Test_MakeSureElementHasStartAndEndTags(
                        string text, string elementName,
                        int iElementStartLine, int iElementStartIndex,
                        string expectedText)
        {
            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer))
                {
                    xamlReader.MakeSureElementHasStartAndEndTags(
                        new AppDotXamlDocument.Location(iElementStartLine, iElementStartIndex),
                        elementName);
                    Assert.AreEqual(expectedText, buffer.Fake_AllText);

                    //Repeating should always be a no-op since it should now contain an end tag
                    xamlReader.MakeSureElementHasStartAndEndTags(
                        new AppDotXamlDocument.Location(iElementStartLine, iElementStartIndex),
                        elementName);
                    Assert.AreEqual(expectedText, buffer.Fake_AllText);
                }
            }
        }

        [TestMethod]
        public void MakeSureElementHasStartAndEndTags()
        {
            Test_MakeSureElementHasStartAndEndTags(
                String.Join("\r\n", new string[]{
                    "<EmptyElementTag/>"
                }),
                "EmptyElementTag",
                0, 0,
                String.Join("\r\n", new string[]{
                    "<EmptyElementTag></EmptyElementTag>"
                })
            );
        }

        [TestMethod]
        public void MakeSureElementHasStartAndEndTags_NonZeroStart()
        {
            Test_MakeSureElementHasStartAndEndTags(
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "  <EmptyElementTag/>"
                }),
                "EmptyElementTag",
                1, 2,
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "  <EmptyElementTag></EmptyElementTag>"
                })
            );
        }

        [TestMethod]
        public void MakeSureElementHasStartAndEndTags_NoClosingAngleBracket()
        {
            try
            {
                Test_MakeSureElementHasStartAndEndTags(
                    String.Join("\r\n", new string[]{
                        "<EmptyElementTag/"
                    }),
                    "EmptyElementTag",
                    0, 0,
                    null);
                Assert.Fail("Expected exception");
            }
            catch (XamlReadWriteException ex)
            {
                Assert.AreEqual("The .xaml file was in an unexpected format, near line 1 column 1.",
                    ex.Message);
            }
        }

        [TestMethod]
        public void MakeSureElementHasStartAndEndTags_WithAttributes()
        {
            Test_MakeSureElementHasStartAndEndTags(
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<EmptyElementTag abc='\"' def=\"'<>//><\"/>"
                }),
                "EmptyElementTag",
                1, 0,
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<EmptyElementTag abc='\"' def=\"'<>//><\"></EmptyElementTag>"
                })
            );
        }

        [TestMethod]
        public void MakeSureElementHasStartAndEndTags_WithAttributesAndWhitespace()
        {
            Test_MakeSureElementHasStartAndEndTags(
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<EmptyElementTag abc='\"' def=\"'<>//><\"",
                    "  />"
                }),
                "EmptyElementTag",
                1, 0,
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<EmptyElementTag abc='\"' def=\"'<>//><\"",
                    "  ></EmptyElementTag>"
                })
            );
        }

        [TestMethod]
        public void MakeSureElementHasStartAndEndTags_ClosedEmpty()
        {
            Test_MakeSureElementHasStartAndEndTags(
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<EmptyElementTag abc='\"' def=\"'<>//><\"",
                    "  ></EmptyElementTag>"
                }),
                "EmptyElementTag",
                1, 0,
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<EmptyElementTag abc='\"' def=\"'<>//><\"",
                    "  ></EmptyElementTag>"
                })
            );
        }

        [TestMethod]
        public void MakeSureElementHasStartAndEndTags_ClosedWithChildren()
        {
            Test_MakeSureElementHasStartAndEndTags(
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<EmptyElementTag abc='\"' def=\"'<>//><\"",
                    "  ><abc><def>",
                    "</def> This is some content. </abc> <child1/>",
                    "<child2>asdf</child2></EmptyElementTag>"
                }),
                "EmptyElementTag",
                1, 0,
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<EmptyElementTag abc='\"' def=\"'<>//><\"",
                    "  ><abc><def>",
                    "</def> This is some content. </abc> <child1/>",
                    "<child2>asdf</child2></EmptyElementTag>"
                })
            );
        }

        [TestMethod]
        public void MakeSureElementHasStartAndEndTags_Whitespace()
        {
            Test_MakeSureElementHasStartAndEndTags(
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<  ",
                    "  EmptyElementTag  ",
                    "abc='\"' def=\"'<>//><\"",
                    "  />"
                }),
                "EmptyElementTag",
                1, 0,
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<  ",
                    "  EmptyElementTag  ",
                    "abc='\"' def=\"'<>//><\"",
                    "  ></EmptyElementTag>"
                })
            );
        }

        [TestMethod]
        public void MakeSureElementHasStartAndEndTags_WhitespaceAfterStartBracket_Closed()
        {
            Test_MakeSureElementHasStartAndEndTags(
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<  ",
                    "  EmptyElementTag  ",
                    "abc='\"' def=\"'<>//><\"",
                    "  ><abc><def>",
                    "</def> This is some content. </abc> <child1/>",
                    "<child2>asdf</child2></EmptyElementTag>"
                }),
                "EmptyElementTag",
                1, 0,
                String.Join("\r\n", new string[]{
                    "<foo>",
                    "<  ",
                    "  EmptyElementTag  ",
                    "abc='\"' def=\"'<>//><\"",
                    "  ><abc><def>",
                    "</def> This is some content. </abc> <child1/>",
                    "<child2>asdf</child2></EmptyElementTag>"
                })
            );
        }


        #endregion

        #region InsertStringAsElementContent
#if false  //undone
        [TestMethod]
        public void InsertStringAsElementContent()
        {
            string text =
                String.Join("\r\n", new string[]{
                    "<EmptyElementTag/>"
                });

            using (VsTextBufferFake buffer = new VsTextBufferFake(text))
            {
                using (AppDotXamlDocument xamlReader = new AppDotXamlDocument(buffer)) {
                string value = xamlReader.GetShutdownMode();
                Assert.AreEqual(expectedCurrentValue, value);

                xamlReader.SetShutdownMode(newText);

                Assert.AreEqual(newText, xamlReader.GetShutdownMode());
                Assert.AreEqual(expectedText, buffer.Fake_AllText);
        }
            }
        }
#endif
        #endregion
    }
}

