// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.CSharp
{
    /// <summary>
    ///     Provides common well-known C# project property pages.
    /// </summary>
    internal static class CSharpProjectDesignerPage
    {
        public static readonly ProjectDesignerPageMetadata Application    = new(new Guid("{5E9A8AC2-4F34-4521-858F-4C248BA31532}"), pageOrder: 0, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata Build          = new(new Guid("{A54AD834-9219-4aa6-B589-607AF21C3E26}"), pageOrder: 1, hasConfigurationCondition: true);
        public static readonly ProjectDesignerPageMetadata BuildEvents    = new(new Guid("{1E78F8DB-6C07-4d61-A18F-7514010ABD56}"), pageOrder: 2, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata Package        = new(new Guid("{21b78be8-3957-4caa-bf2f-e626107da58e}"), pageOrder: 3, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata Debug          = new(new Guid("{0273C280-1882-4ED0-9308-52914672E3AA}"), pageOrder: 4, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata ReferencePaths = new(new Guid("{031911C8-6148-4e25-B1B1-44BCA9A0C45C}"), pageOrder: 5, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata Signing        = new(new Guid("{F8D6553F-F752-4DBF-ACB6-F291B744A792}"), pageOrder: 6, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata CodeAnalysis   = new(new Guid("{c02f393c-8a1e-480d-aa82-6a75d693559d}"), pageOrder: 7, hasConfigurationCondition: false);
    }
}
