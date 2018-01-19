using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class ExceptionViewModel : BaseViewModel
    {
        private readonly Exception _exception;

        public override string Text => _exception.Message;

        public ExceptionViewModel(Exception exception)
        {
            _exception = exception;
        }
    }
}
