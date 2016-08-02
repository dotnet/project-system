// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    ///     Provides common well-known Visual Basic project property pages.
    /// </summary>
    internal static class VisualBasicProjectDesignerPage
    {
        public static readonly ProjectDesignerPageMetadata Application = new ProjectDesignerPageMetadata(new Guid("{8998E48E-B89A-4034-B66E-353D8C1FDC2E}"), pageOrder:0, hasConfigurationCondition:false);
        public static readonly ProjectDesignerPageMetadata References = new ProjectDesignerPageMetadata(new Guid("{4E43F4AB-9F03-4129-95BF-B8FF870AF6AB}"), pageOrder: 1, hasConfigurationCondition: false);
    }
}
