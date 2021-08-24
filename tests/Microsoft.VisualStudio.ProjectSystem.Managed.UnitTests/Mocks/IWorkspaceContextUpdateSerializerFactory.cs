// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IWorkspaceContextUpdateSerializerFactory
    {
        public static IWorkspaceContextUpdateSerializer Create()
        {
            var mock = new Mock<IWorkspaceContextUpdateSerializer>();

            mock.Setup(ser => ser.ApplyUpdateAsync(It.IsAny<Func<Task>>()))
                .Returns(new Func<Func<Task>, Task>(callback => callback()));

            return mock.Object;
        }
    }
}
