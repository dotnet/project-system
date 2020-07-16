// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    // Sadly, in some versions of MSBuild, evaluationIds are not unique, and
    // so you may end up with more than one project evaluation being assigned
    // a single evaluationId
    internal sealed class EvaluationInfo : BaseInfo
    {
        private List<EvaluatedProjectInfo> _evaluatingProjects;

        private List<EvaluatedProjectInfo> _evaluatedProjects;

        public IReadOnlyList<EvaluatedProjectInfo> EvaluatedProjects => _evaluatedProjects;

        public void StartEvaluatingProject(EvaluatedProjectInfo evaluatedProject)
        {
            if (_evaluatingProjects == null)
            {
                _evaluatingProjects = new List<EvaluatedProjectInfo>();
            }

            _evaluatingProjects.Add(evaluatedProject);
        }

        public EvaluatedProjectInfo EndEvaluatingProject(string name)
        {
            EvaluatedProjectInfo evaluatedProject = _evaluatingProjects.FirstOrDefault(p => p.Name == name);
            if (evaluatedProject == null)
            {
                throw new LoggerException(Resources.CannotFindEvaluatedProject);
            }

            if (_evaluatedProjects == null)
            {
                _evaluatedProjects = new List<EvaluatedProjectInfo>();
            }

            _evaluatedProjects.Add(evaluatedProject);
            _evaluatingProjects.Remove(evaluatedProject);
            return evaluatedProject;
        }
    }
}
