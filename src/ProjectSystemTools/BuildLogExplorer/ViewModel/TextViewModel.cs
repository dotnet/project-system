// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class TextViewModel : BaseViewModel
    {
        public override string Text { get; }

        public TextViewModel(string name)
        {
            Text = name;
        }
    }
}
