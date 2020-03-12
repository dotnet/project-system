// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal partial class DebugPageViewModel
    {
        public class LaunchType
        {
            public string CommandName { get; }
            public string Name { get; }

            public LaunchType(string commandName, string name)
            {
                CommandName = commandName;
                Name = name;
            }

            public override bool Equals(object obj)
            {
                if (obj is LaunchType oth)
                {
                    return CommandName?.Equals(oth.CommandName) ?? oth.CommandName == null;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return CommandName?.GetHashCode() ?? 0;
            }
        }
    }
}
