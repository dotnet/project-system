// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
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
