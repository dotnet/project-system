// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class UnitTestHelper
    {
        /// <summary>
        /// This property helps to alter behavior when in unit test mode,
        /// for example not throw or not switch to UI thread etc.
        /// </summary>
        public static bool IsRunningUnitTests { get; set; }
    }
}
