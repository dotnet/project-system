// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Resources;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections.Implementation
{
    /// <summary>
    /// Specifies the localized display name for a property, event or public void method which takes no arguments.
    /// </summary>
    internal sealed class BrowseObjectDisplayNameAttribute : BrowseObjectDisplayNameAttributeBase
    {
        public BrowseObjectDisplayNameAttribute(string key)
            : base(key)
        { }

        protected override ResourceManager ResourceManager => VSResources.ResourceManager;
    }
}
