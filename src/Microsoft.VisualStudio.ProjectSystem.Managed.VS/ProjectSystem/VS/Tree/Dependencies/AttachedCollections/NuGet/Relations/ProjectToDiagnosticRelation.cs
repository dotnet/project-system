// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    [Export(typeof(IRelation))]
    internal sealed class ProjectToDiagnosticRelation : RelationBase<ProjectReferenceItem, DiagnosticItem>
    {
        protected override bool HasContainedItems(ProjectReferenceItem parent)
        {
            return parent.Target.Logs.Any(log => log.LibraryName == parent.Library.Name);
        }

        protected override void UpdateContainsCollection(ProjectReferenceItem parent, AggregateContainsRelationCollectionSpan span)
        {
            span.UpdateContainsItems(
                parent.Target.Logs.Where(log => log.LibraryName == parent.Library.Name).OrderBy(log => log.LibraryName).ThenBy(log => log.Message),
                (log, item) => StringComparer.Ordinal.Compare(log.LibraryName, item.Library.Name),
                (log, item) => item.TryUpdateState(parent.Target, parent.Library, log),
                log => new DiagnosticItem(parent.Target, parent.Library, log));
        }

        protected override IEnumerable<ProjectReferenceItem>? CreateContainedByItems(DiagnosticItem child)
        {
            yield return new ProjectReferenceItem(child.Target, child.Library);
        }
    }
}
