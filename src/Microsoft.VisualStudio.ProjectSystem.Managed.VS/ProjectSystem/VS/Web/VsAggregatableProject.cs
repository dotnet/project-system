// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell.Flavor;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsAggregatableProjectCorrected"/> to handle 
    ///     designers and features that sniff for the project type via 
    ///     <see cref="IVsAggregatableProjectCorrected.GetAggregateProjectTypeGuids"/>.
    /// </summary>
    [ExportProjectNodeComService(typeof(IVsAggregatableProjectCorrected))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class VsAggregatableProject : IVsAggregatableProjectCorrected, IDisposable
    {
        private IProjectThreadingService? _threadingService;
        private IActiveConfiguredValue<ProjectTypeGuidsDataSource>? _dataSource;

        [ImportingConstructor]
        public VsAggregatableProject(IProjectThreadingService projectThreadingService, IActiveConfiguredValue<ProjectTypeGuidsDataSource> dataSource)
        {
            _threadingService = projectThreadingService;
            _dataSource = dataSource;
        }

        public int SetInnerProject(IntPtr punkInnerIUnknown)
        {
            return HResult.NotImplemented;
        }

        public int InitializeForOuter(string pszFilename, string pszLocation, string pszName, uint grfCreateFlags, ref Guid iidProject, out IntPtr ppvProject, out int pfCanceled)
        {
            ppvProject = default;
            pfCanceled = 0;
            return HResult.NotImplemented;
        }

        public int OnAggregationComplete()
        {
            return HResult.NotImplemented;
        }

        public int GetAggregateProjectTypeGuids(out string? pbstrProjTypeGuids)
        {
            IImmutableList<Guid>? projectTypes = _threadingService?.ExecuteSynchronously(() =>
            {
                return _dataSource?.Value.GetProjectTypeGuids()!;
            });

            if (projectTypes != null)
            {
                pbstrProjTypeGuids = string.Join(";", projectTypes.Select(projectType => projectType.ToString("B")));
                return HResult.OK;
            }

            // Disposed
            pbstrProjTypeGuids = null;
            return HResult.Unexpected;
        }

        public int SetAggregateProjectTypeGuids(string lpstrProjTypeGuids)
        {
            return HResult.NotImplemented;
        }

        public void Dispose()
        {
            // Important for ProjectNodeComServices to null out fields to reduce the amount 
            // of data we leak when extensions incorrectly holds onto the IVsHierarchy.
            _threadingService = null;
            _dataSource = null;
        }
    }
}
