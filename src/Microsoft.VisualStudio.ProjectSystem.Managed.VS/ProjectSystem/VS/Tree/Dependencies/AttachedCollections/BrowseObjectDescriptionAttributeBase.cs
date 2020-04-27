// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Specifies a localized description for a property or event.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class BrowseObjectDescriptionAttributeBase : DescriptionAttribute
    {
        private readonly string _key;

        protected BrowseObjectDescriptionAttributeBase(string key) => _key = key;

        public override string Description
        {
            get
            {
                // Defer lookup and cache in base class's DescriptionValue field
                string name = base.Description;

                if (name.Length == 0)
                {
                    name = DescriptionValue = ResourceManager.GetString(_key, CultureInfo.CurrentUICulture);
                }

                return name;
            }
        }

        protected abstract ResourceManager ResourceManager { get; }
    }
}
