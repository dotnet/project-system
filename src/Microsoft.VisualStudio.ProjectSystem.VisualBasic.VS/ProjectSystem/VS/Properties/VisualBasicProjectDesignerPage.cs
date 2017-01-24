// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    ///     Provides common well-known Visual Basic project property pages.
    /// </summary>
    internal static class VisualBasicProjectDesignerPage
    {
        public static readonly ProjectDesignerPageMetadata Application = new ProjectDesignerPageMetadata(new Guid("{8998E48E-B89A-4034-B66E-353D8C1FDC2E}"), pageOrder: 0, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata Package = new ProjectDesignerPageMetadata(new Guid("{21b78be8-3957-4caa-bf2f-e626107da58e}"), pageOrder: 0, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata References = new ProjectDesignerPageMetadata(new Guid("{4E43F4AB-9F03-4129-95BF-B8FF870AF6AB}"), pageOrder: 1, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata Debug = new ProjectDesignerPageMetadata(new Guid("{0273C280-1882-4ED0-9308-52914672E3AA}"), pageOrder: 2, hasConfigurationCondition: true);
    }
}
