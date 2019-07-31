// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Exports a service to be aggregated with the project node.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface, AllowMultiple = true)]
    internal class ExportProjectNodeComServiceAttribute : ExportAttribute
    {
        public ExportProjectNodeComServiceAttribute(params Type[] comTypes)
        : base(ExportContractNames.VsTypes.ProjectNodeComExtension)
        {
            Iid = GetIids(comTypes);
        }

        public string[] Iid
        {
            get;
        }

        public static bool IncludeInherited
        {
            get => false;
        }

        private static string[] GetIids(Type[] comTypes)
        {
            Requires.NotNull(comTypes, nameof(comTypes));

            // Reuse ComServiceIdAttribute's logic for calculating IIDs.
            return comTypes.Select(type => new ComServiceIidAttribute(type))
                           .SelectMany(attribute => attribute.Iid)
                           .ToArray();
        }
    }
}
