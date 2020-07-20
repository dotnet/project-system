// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    /// <summary>
    /// Static class containing helper utilities for working with UI thread.
    /// </summary>
    internal static class UIThreadHelper
    {
        /// <summary>
        /// Helper utility to ensure that we are on UI thread. Needs to be called
        /// in every method/property with UI thread affinity (to avoid hangs
        /// which are hard to repro and investigate).
        /// </summary>
        public static void VerifyOnUIThread([CallerMemberName] string memberName = "")
        {
#if DEBUG
            try
            {
#pragma warning disable RS0030 // Do not used banned APIs
                ThreadHelper.ThrowIfNotOnUIThread(memberName);
#pragma warning restore RS0030 // Do not used banned APIs
            }
            catch
            {
                System.Diagnostics.Debug.Fail("Call made on the Non-UI thread by " + memberName);
                throw;
            }
#else
#pragma warning disable RS0030 // Do not used banned APIs
            ThreadHelper.ThrowIfNotOnUIThread(memberName);
#pragma warning restore RS0030 // Do not used banned APIs
#endif
        }
    }
}
