// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModelMethods.Actions;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// <see cref="IQueryActionExecutor"/> handling <see cref="ProjectModelActionNames.SetEvaluatedUIPropertyValue"/> actions.
    /// </summary>
    internal sealed class ProjectSetEvaluatedUIPropertyValueAction : ProjectSetUIPropertyValueActionBase<object?>
    {
        private readonly SetEvaluatedUIPropertyValue _parameter;

        public ProjectSetEvaluatedUIPropertyValueAction(SetEvaluatedUIPropertyValue parameter)
            : base(parameter.Page, parameter.Name, parameter.Dimensions)
        {
            Requires.NotNull(parameter, nameof(parameter));
            Requires.NotNull(parameter.Dimensions, $"{nameof(parameter)}.{nameof(parameter.Dimensions)}");

            _parameter = parameter;
        }

        protected override Task SetValueAsync(IProperty property)
        {
            return property.SetValueAsync(_parameter.Value);
        }
    }
}
