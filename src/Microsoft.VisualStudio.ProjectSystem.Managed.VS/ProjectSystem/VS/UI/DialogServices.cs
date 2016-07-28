// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    /// <summary>
    /// Provides an abstraction over dialogs to make them unit testable. Each dialog will have its own abstraction which
    /// can be retrieved from this servcie. 
    /// </summary>
    [Export(typeof(IDialogServices))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class DialogServices : IDialogServices
    {
        [ImportingConstructor]
        public DialogServices([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private readonly IServiceProvider _serviceProvider;

        public MultiChoiceMsgBoxResult ShowMultiChoiceMsgBox(string dialogTitle, string errorText, string[] buttons)
        { 
            var dlg = new MultiChoiceMsgBox(dialogTitle, errorText, buttons);
            var result = dlg.ShowModal();
            if(result == true)
            {
                return dlg.SelectedAction;
            }
            return MultiChoiceMsgBoxResult.Cancel;
        }
    }
}
