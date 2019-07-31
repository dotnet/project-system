// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
