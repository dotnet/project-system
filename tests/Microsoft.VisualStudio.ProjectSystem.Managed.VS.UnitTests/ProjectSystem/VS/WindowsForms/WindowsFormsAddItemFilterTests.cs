using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms
{
    public class WindowsFormsAddItemFilterTests
    {
        [Theory]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\Non-Windows Forms", false })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\General", false })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Extensions\dh64yf.343\ProjectTemplates\CSharp", false })]
        [InlineData(new object[] { @"C:\Program Files\Visual Studio\Templates\Windows Forms", true })]
        public void FiltersItemTemplateFolders(string templateDir, bool shouldBeFiltered)
        {
            var filter = new WindowsFormsAddItemFilter();

            Guid guid = Guid.Empty;
            var result = filter.FilterTreeItemByTemplateDir(ref guid, templateDir, out int filterResult);

            Assert.Equal(0, result);
            Assert.Equal(shouldBeFiltered, (filterResult == 1));
        }
    }
}
