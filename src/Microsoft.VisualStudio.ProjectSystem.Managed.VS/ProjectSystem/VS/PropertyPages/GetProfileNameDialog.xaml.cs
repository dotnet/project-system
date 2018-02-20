// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    [ExcludeFromCodeCoverage]
    public partial class GetProfileNameDialog : DialogWindow
    {
        private readonly Predicate<string> _validator;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IProjectThreadingService _threadingService;

        public string ProfileName { get; set; }

        public GetProfileNameDialog(SVsServiceProvider sp, IProjectThreadingService threadingService, string suggestedName, Predicate<string> validator)
            : base()// Pass help topic to base if there is one
        {
            InitializeComponent();
            DataContext = this;
            ProfileName = suggestedName;
            _validator = validator;
            _serviceProvider = sp;
            _threadingService = threadingService;
        }

        //------------------------------------------------------------------------------
        // Validate the name is valid
        //------------------------------------------------------------------------------
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            string newName = ProfileName;
            newName = newName?.Trim();
            var notifyService = new UserNotificationServices(_serviceProvider, _threadingService);

            if (string.IsNullOrEmpty(newName))
            {
                notifyService.ShowMessageBox(PropertyPageResources.ProfileNameRequired, null, OLEMSGICON.OLEMSGICON_CRITICAL,
                                                  OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            else if (!_validator(newName))
            {
                notifyService.ShowMessageBox(PropertyPageResources.ProfileNameInvalid, null, OLEMSGICON.OLEMSGICON_CRITICAL,
                                                  OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            else
            {
                ProfileName = newName;
                DialogResult = true;
            }
        }

        //------------------------------------------------------------------------------
        // Returns the name of the current product we are instantiated in from the appropriate resource
        // Used for dialog title binding
        //------------------------------------------------------------------------------
        public string DialogCaption
        {
            get
            {
                return PropertyPageResources.NewProfileCaption;
            }
        }

        //------------------------------------------------------------------------------
        // Called when window loads. Use it to set focus on the text box correctly.
        //------------------------------------------------------------------------------
        private delegate void SetFocusCallback();
        private void GetProfileNameDialogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // We need to schedule this to occur later after databinding has completed, otherwise
            // focus appears in the textbox, but at the start of the suggested name rather than at
            // the end.
#pragma warning disable VSTHRD001 // Avoid legacy threading switching APIs. 
            // see https://github.com/Microsoft/vs-threading/issues/138
            // There is currently no better way to queue an item on 
            // DispatcherPriority.DataBind until the above issue is fixed
            Dispatcher.BeginInvoke(
                    (SetFocusCallback)delegate ()
                    {
                        ProfileNameTextBox.Select(0, ProfileNameTextBox.Text.Length);
                        ProfileNameTextBox.Focus();
                    }, System.Windows.Threading.DispatcherPriority.DataBind, null);
#pragma warning restore VSTHRD001 // Avoid legacy threading switching APIs
        }
    }
}
