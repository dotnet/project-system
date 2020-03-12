// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    /// <summary>
    /// Provides an abstraction over dialogs to make them unit testable. Each dialog will have its own abstraction which
    /// can be retrieved from this service.
    /// </summary>
    [Export(typeof(IDialogServices))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class DialogServices : IDialogServices
    {
        private readonly IUserNotificationServices _userNotificationServices;

        [ImportingConstructor]
        public DialogServices(IUserNotificationServices userNotificationServices)
        {
            _userNotificationServices = userNotificationServices;
        }

        public bool DontShowAgainMessageBox(string caption, string message, string? checkboxText, bool initialStateOfCheckbox, string learnMoreText, string learnMoreUrl)
        {
            var dlg = new DontShowAgainMessageBox(caption, message, checkboxText, initialStateOfCheckbox, learnMoreText, learnMoreUrl, _userNotificationServices);
            bool? result = dlg.ShowModal();
            if (result == true)
            {
                return dlg.CheckboxState;
            }

            return false;
        }
    }
}
