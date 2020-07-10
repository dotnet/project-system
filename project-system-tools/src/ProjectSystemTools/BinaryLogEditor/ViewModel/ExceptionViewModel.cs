// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
