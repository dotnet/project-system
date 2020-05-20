// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = Composition.ImportCardinality.ExactlyOne)]
    internal interface IProjectRetargetingManager
    {
        void ReportProjectMightRetarget(string projectFile);
        void ReportProjectNeedsRetargeting(string projectFile, IEnumerable<ProjectTargetChange> changes);
    }
}
