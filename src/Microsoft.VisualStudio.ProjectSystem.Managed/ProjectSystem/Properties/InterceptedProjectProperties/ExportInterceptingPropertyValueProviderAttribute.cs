// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Exports a <see cref="IInterceptingPropertyValueProvider"/> extension to CPS.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class ExportInterceptingPropertyValueProviderAttribute : ExportAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportInterceptingPropertyValueProviderAttribute"/> class.
        /// </summary>
        public ExportInterceptingPropertyValueProviderAttribute()
            : base(typeof(IInterceptingPropertyValueProvider))
        {
        }
    }
}
