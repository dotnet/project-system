// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.FSharp
{
    /// <summary>
    ///     Provides common well-known F# project property pages.
    /// </summary>
    internal static class FSharpProjectDesignerPage
    {
        public static readonly ProjectDesignerPageMetadata Application    = new ProjectDesignerPageMetadata(new Guid("{6D2D9B56-2691-4624-A1BF-D07A14594748}"), pageOrder: 0, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata Build          = new ProjectDesignerPageMetadata(new Guid("{FAC0A17E-2E70-4211-916A-0D34FB708BFF}"), pageOrder: 1, hasConfigurationCondition: true);
        public static readonly ProjectDesignerPageMetadata BuildEvents    = new ProjectDesignerPageMetadata(new Guid("{DD84AA8F-71BB-462a-8EF8-C9992CB325B7}"), pageOrder: 2, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata Debug          = new ProjectDesignerPageMetadata(new Guid("{0273C280-1882-4ED0-9308-52914672E3AA}"), pageOrder: 3, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata Package        = new ProjectDesignerPageMetadata(new Guid("{21b78be8-3957-4caa-bf2f-e626107da58e}"), pageOrder: 4, hasConfigurationCondition: false);
        public static readonly ProjectDesignerPageMetadata ReferencePaths = new ProjectDesignerPageMetadata(new Guid("{DF16B1A2-0E91-4499-AE60-C7144E614BF1}"), pageOrder: 5, hasConfigurationCondition: false);
    }
}
