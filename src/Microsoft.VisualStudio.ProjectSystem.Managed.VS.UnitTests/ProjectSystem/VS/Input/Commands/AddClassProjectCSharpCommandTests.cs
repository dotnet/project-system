// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectSystemTrait]
    public class AddClassProjectCSharpCommandTests : AbstractAddClassProjectCommandTests
    {
        internal override string DirName { get; } = "Visual C# Items";

        internal override AbstractAddClassProjectCommand CreateInstance(IPhysicalProjectTree tree, IUnconfiguredProjectVsServices services, SVsServiceProvider serviceProvider) =>
            new AddClassProjectCSharpCommand(tree, services, serviceProvider);
    }
}
