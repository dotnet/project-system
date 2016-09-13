// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Used to get to the JsonString exported attribute by importers of ILaunchSettingsSerializationProvider
    /// </summary>
    public interface IJsonSection : IOrderPrecedenceMetadataView
    {
        [DefaultValue(null)]
        string JsonSection { get; }

        [DefaultValue(null)]
        Type SerializationType { get; }
    }
}
