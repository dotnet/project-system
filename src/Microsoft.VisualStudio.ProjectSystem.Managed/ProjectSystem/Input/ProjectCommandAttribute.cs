// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Input
{
    /// <summary>
    ///     Specifies the command group and ID of a given <see cref="AbstractProjectCommand"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface, AllowMultiple = false)]
    [MetadataAttribute]
    internal class ProjectCommandAttribute : ExportAttribute
    {
        public ProjectCommandAttribute(string group, long commandId)
            : this(group, new long[] { commandId })
        {
        }

        public ProjectCommandAttribute(string group, params long[] commandIds)
            : base(typeof(IAsyncCommandGroupHandler))
        {
            Group = new Guid(group);
            CommandIds = commandIds;
        }

        public long[] CommandIds { get; }

        public Guid Group { get; }
    }
}
