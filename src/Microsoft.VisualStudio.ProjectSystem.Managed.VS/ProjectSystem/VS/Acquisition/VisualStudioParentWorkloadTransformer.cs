// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Acquisition
{
    /// <summary>
    ///     Transforms a collection of Visual Studio component IDs.
    /// </summary>
    [Export(typeof(IVisualStudioComponentIdTransformer))]
    internal class VisualStudioParentWorkloadTransformer : IVisualStudioComponentIdTransformer
    {
        private readonly Dictionary<string, ISet<string>> _vsComponentIdToParentComponentsMap;

        [ImportingConstructor]
        public VisualStudioParentWorkloadTransformer()
        {
            _vsComponentIdToParentComponentsMap = new(StringComparers.VisualStudioSetupComponentIdComparer);
        }

        public Task<IReadOnlyCollection<string>> TransformVisualStudioComponentIdsAsync(IReadOnlyCollection<string> vsComponentIds)
        {
            return Task.FromResult(vsComponentIds);
        }
    }
}
