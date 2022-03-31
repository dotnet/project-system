// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    public class ProjectDesignerPageMetadataTests
    {
        [Fact]
        public void Constructor_GuidEmptyAsPageGuid_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("pageGuid", () =>
            {
                new ProjectDesignerPageMetadata(Guid.Empty, 0, hasConfigurationCondition: false);
            });
        }

        [Fact]
        public void Constructor_ValueAsPageGuid_SetsPageGuidProperty()
        {
            var guid = Guid.NewGuid();
            var metadata = new ProjectDesignerPageMetadata(guid, 0, hasConfigurationCondition: false);

            Assert.Equal(guid, metadata.PageGuid);
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-10)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(int.MaxValue)]
        public void Constructor_ValueAsPageOrder_SetsPageOrderProperty(int pageOrder)
        {
            var metadata = new ProjectDesignerPageMetadata(Guid.NewGuid(), pageOrder, hasConfigurationCondition: false);

            Assert.Equal(pageOrder, metadata.PageOrder);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Constructor_ValueAsHasConfigurationCondition_SetsHasConfigurationConditionProperty(bool hasConfigurationCondition)
        {
            var metadata = new ProjectDesignerPageMetadata(Guid.NewGuid(), 0, hasConfigurationCondition: hasConfigurationCondition);

            Assert.Equal(hasConfigurationCondition, metadata.HasConfigurationCondition);
        }

        [Fact]
        public void Name_ReturnsNull()
        {
            var metadata = CreateInstance();

            Assert.Null(metadata.Name);
        }

        private static ProjectDesignerPageMetadata CreateInstance()
        {
            return new ProjectDesignerPageMetadata(Guid.NewGuid(), 0, false);
        }
    }
}
