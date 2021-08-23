// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal class SetAttributeCommand : SetTreatAsUsedAttributeCommand
    {
        public SetAttributeCommand(AbstractReferenceHandler abstractReferenceHandler, ConfiguredProject selectedConfiguredProject, string itemSpecification)
        : base(abstractReferenceHandler, selectedConfiguredProject, itemSpecification)
        {
            UnsetTreatAsUsed = PropertySerializer.SimpleTypes.ToString(false);
            SetTreatAsUsed = PropertySerializer.SimpleTypes.ToString(true);
        }
    }
}
