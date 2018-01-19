using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal abstract class BaseViewModel
    {
        public abstract string Text { get; }

        public virtual IEnumerable<object> Children => Enumerable.Empty<object>();

        public virtual SelectedObjectWrapper Properties => null;
    }
}
