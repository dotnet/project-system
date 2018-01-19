using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class RootViewModel
    {
        private readonly LogModel.Build _build;
        private readonly IEnumerable<Exception> _exceptions;
        private IEnumerable<object> _children;

        public IEnumerable<object> Children => _children ?? (_children = GetChildren());

        public RootViewModel(LogModel.Build build)
        {
            _build = build;
        }

        public RootViewModel(IEnumerable<Exception> exceptions)
        {
            _exceptions = exceptions;
        }

        private List<object> GetChildren()
        {
            List<object> list;

            if (_build != null)
            {
                list = new List<object>
                {
                    new BuildViewModel(_build)
                };
            }
            else
            {
                list = _exceptions.Select(ex => (object)new ExceptionViewModel(ex)).ToList();
            }

            return list;
        }
    }
}
