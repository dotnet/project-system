// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ICommandLineArgumentsDataSourceFactory
    {
        public static ICommandLineArgumentsDataSource Create()
        {
            var sourceBlock = new Mock<IReceivableSourceBlock<IProjectVersionedValue<CommandLineArgumentsSnapshot>>>().Object;

            var mock = new Mock<ICommandLineArgumentsDataSource>();
            mock.SetupGet(s => s.SourceBlock)
                .Returns(sourceBlock);

            return mock.Object;
        }
    }
}
