// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms
{
    public class WindowsFormsAddItemFilterTests
    {
        [Theory]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\Non-Windows Forms", false })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\General", false })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Extensions\dh64yf.343\ProjectTemplates\CSharp", false })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\Windows Forms", true })]
        public void FiltersItemTemplateFolders_NonWindowsForms(string templateDir, bool shouldBeFiltered)
        {
            var filter = new WindowsFormsAddItemFilter(UnconfiguredProjectFactory.Create(scope: IProjectCapabilitiesScopeFactory.Create()));

            Guid guid = Guid.Empty;
            var result = filter.FilterTreeItemByTemplateDir(ref guid, templateDir, out int filterResult);

            Assert.Equal(0, result);
            Assert.Equal(shouldBeFiltered, filterResult == 1);
        }

        [Theory]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\Non-Windows Forms", false })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\General", false })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Extensions\dh64yf.343\ProjectTemplates\CSharp", false })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\Windows Forms", false })]
        public void FiltersItemTemplateFolders_WindowsForms(string templateDir, bool shouldBeFiltered)
        {
            var filter = new WindowsFormsAddItemFilter(UnconfiguredProjectFactory.Create(scope: IProjectCapabilitiesScopeFactory.Create(new string[] { "WindowsForms" })));

            Guid guid = Guid.Empty;
            var result = filter.FilterTreeItemByTemplateDir(ref guid, templateDir, out int filterResult);

            Assert.Equal(0, result);
            Assert.Equal(shouldBeFiltered, filterResult == 1);
        }

        [Theory]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\CSControlInheritanceWizard.vsz", true })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\CSFormInheritanceWizard.vsz", true })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\InheritedForm.vsz", true })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\InheritedControl.vsz", true })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\Class.vsz", false })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\ControlLibrary.vsz", false })]
        public void FiltersItemTemplateFiles(string templateDir, bool shouldBeFiltered)
        {
            var filter = new WindowsFormsAddItemFilter(UnconfiguredProjectFactory.Create());

            Guid guid = Guid.Empty;
            var result = filter.FilterListItemByTemplateFile(ref guid, templateDir, out int filterResult);

            Assert.Equal(0, result);
            Assert.Equal(shouldBeFiltered, filterResult == 1);
        }
    }
}
