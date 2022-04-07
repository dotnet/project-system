// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public class OpenProjectDesignerOnDefaultActionCommandTests : AbstractOpenProjectDesignerCommandTests
    {
        [Fact]
        public void Constructor_NullAsDesignerService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("designerService", () =>
            {
                new OpenProjectDesignerOnDefaultActionCommand(null!);
            });
        }

        internal override long GetCommandId()
        {
            return UIHierarchyWindowCommandId.DoubleClick;
        }

        internal override AbstractOpenProjectDesignerCommand CreateInstance(IProjectDesignerService? designerService = null)
        {
            designerService ??= IProjectDesignerServiceFactory.Create();

            return new OpenProjectDesignerOnDefaultActionCommand(designerService);
        }
    }
}
