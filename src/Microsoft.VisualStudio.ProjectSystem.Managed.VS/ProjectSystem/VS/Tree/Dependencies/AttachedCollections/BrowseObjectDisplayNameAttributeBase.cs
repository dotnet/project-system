// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Specifies the localized display name for a property, event or public void method which takes no arguments.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class BrowseObjectDisplayNameAttributeBase : DisplayNameAttribute
    {
        private readonly string _key;

        protected BrowseObjectDisplayNameAttributeBase(string key) => _key = key;

        public override string DisplayName
        {
            get
            {
                // Defer lookup and cache in base class's DisplayNameValue field
                string name = base.DisplayName;

                if (name.Length == 0)
                {
                    name = DisplayNameValue = ResourceManager.GetString(_key, CultureInfo.CurrentUICulture);
                }

                return name;
            }
        }

        protected abstract ResourceManager ResourceManager { get; }
    }
}
