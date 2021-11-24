// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.IO
{
    [Export(typeof(IRegistry))]
    internal class WindowsRegistry : IRegistry
    {
        public object? ReadValueForCurrentUser(string keyPath, string name)
        {
            using RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(keyPath);

            if (registryKey is not null)
            {
                try
                {
                    return registryKey.GetValue(name);
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
