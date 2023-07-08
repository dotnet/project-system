// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Used to get to the JsonString exported attribute by importers of <see cref="ILaunchSettingsSerializationProvider"/>.
    /// </summary>
    public interface IJsonSection : IOrderPrecedenceMetadataView
    {
        [DefaultValue(null)]
        string JsonSection { get; }

        [DefaultValue(null)]
        Type SerializationType { get; }
    }
}
