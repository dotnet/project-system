// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public partial class ComponentVerificationTests
    {
        internal class AllExportsTestData : TheoryData<Type>
        {
            public AllExportsTestData()
            {
                var types = from assembly in ComponentComposition.BuiltInAssemblies
                            from type in assembly.GetTypes()
                            where type.GetCustomAttributes<ExportAttribute>().Any()
                            select type;
                foreach (Type type in types)
                {
                    Add(type);
                }
            }
        }
    }
}
