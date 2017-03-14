// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal interface IEnvironmentOptions
    {
        /// <summary>
        /// Provides access to Visual Studio Tools - > Options - > Properties Value.
        /// </summary>
        T GetPropertiesValue<T>(string category, string page, string property, T defaultValue);
    }
}
