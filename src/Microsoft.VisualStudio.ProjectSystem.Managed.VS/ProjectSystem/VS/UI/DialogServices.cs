// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

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
        public DialogServices(IProjectThreadingService threadHandling)
        {
            _threadHandling = threadHandling;
        }

        // Only here to provide scope
        private readonly IProjectThreadingService _threadHandling;

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
