// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.DotNet.Test.PropertyPages
{
    [ProjectSystemTrait]
    public class PropertyPageTests
    {
        [Fact]
        public void PropertyPage_GetPageInfoAndHelp()
        {
            Castle.DynamicProxy.Generators.AttributesToAvoidReplicating.Add(typeof(System.Security.Permissions.UIPermissionAttribute));

            Mock<PropertyPage> page = new Mock<PropertyPage>(false);
            page.Protected().Setup<string>("PropertyPageName").Returns("MyPage");
            PROPPAGEINFO[] pageInfoArray = new PROPPAGEINFO[1];
            page.Object.GetPageInfo(pageInfoArray);
            page.Object.Help(String.Empty);

            PROPPAGEINFO info = pageInfoArray[0];
            Assert.Equal("MyPage", info.pszTitle);
            Assert.Equal(0u, info.dwHelpContext);
            Assert.Equal(info.pszDocString,null);
            Assert.Equal(info.pszHelpFile,null);
            Assert.Equal(page.Object.Size.Width, info.SIZE.cx);
            Assert.Equal(page.Object.Size.Height, info.SIZE.cy);
        }

        [Fact]
        public void PropertyPage_Move()
        {
            Castle.DynamicProxy.Generators.AttributesToAvoidReplicating.Add(typeof(System.Security.Permissions.UIPermissionAttribute));

            RECT[] rect = new RECT[] { new RECT() { left = 25, top = 25 } };
            Mock<PropertyPage> page = new Mock<PropertyPage>(false);
            page.CallBase = true;
            page.Object.Move(rect);

            Assert.Equal(rect[0].left, page.Object.Location.X);
            Assert.Equal(rect[0].top, page.Object.Location.Y);
        }

        [Fact]
        public void PropertyPage_MoveThrowsArgumentExceptionIfNullRect()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                Move(null);
            });
        }

        [Fact]
        public void PropertyPage_MoveThrowsArgumentExceptionIfEmptyRect()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                Move(new RECT[0]);
            });
        }

        private static void Move(RECT[] x)
        {
            Castle.DynamicProxy.Generators.AttributesToAvoidReplicating.Add(typeof(System.Security.Permissions.UIPermissionAttribute));

            Mock<PropertyPage> page = new Mock<PropertyPage>(false);
            page.CallBase = true;

            page.Object.Move(x);
        }
    }
}
