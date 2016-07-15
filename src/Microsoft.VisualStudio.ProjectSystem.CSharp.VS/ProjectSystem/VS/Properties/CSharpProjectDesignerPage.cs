// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    ///     Provides common well-known C# project property pages.
    /// </summary>
    internal static class CSharpProjectDesignerPage
    {
        public static readonly ProjectDesignerPageMetadata Application = new ProjectDesignerPageMetadata(new Guid("{5E9A8AC2-4F34-4521-858F-4C248BA31532}"), pageOrder:0, hasConfigurationCondition:false);
        public static readonly ProjectDesignerPageMetadata Signing = new ProjectDesignerPageMetadata(new Guid("{F8D6553F-F752-4DBF-ACB6-F291B744A792}"), pageOrder: 1, hasConfigurationCondition: false);
    }
}
