// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public class OpenProjectDesignerCommandTests : AbstractOpenProjectDesignerCommandTests
    {
        [Fact]
        public void Constructor_NullAsDesignerService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("designerService", () =>
            {
                new OpenProjectDesignerCommand(null!);
            });
        }

        internal override long GetCommandId()
        {
            return VisualStudioStandard97CommandId.Open;
        }

        internal override AbstractOpenProjectDesignerCommand CreateInstance(IProjectDesignerService? designerService = null)
        {
            designerService ??= IProjectDesignerServiceFactory.Create();

            return new OpenProjectDesignerCommand(designerService);
        }
    }
}
