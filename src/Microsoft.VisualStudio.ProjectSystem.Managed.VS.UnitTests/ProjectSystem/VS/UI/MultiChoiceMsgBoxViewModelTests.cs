// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    [Trait("UnitTest", "ProjectSystem")]
    public class MultiChoiceMsgBoxViewModelTests
    {
        [Fact]
        public void Constructor_ValidateButtons()
        {
            Assert.Throws<ArgumentException>(() => new MultiChoiceMsgBoxViewModel("title", "errorText", new string[0])); 
            Assert.Throws<ArgumentException>(() => new MultiChoiceMsgBoxViewModel("title", "errorText", new string[5])); 
            
            // These should be good
            new MultiChoiceMsgBoxViewModel("title", "errorText", new string[1]); 
            new MultiChoiceMsgBoxViewModel("title", "errorText", new string[4]); 
        }

        [Fact]
        public void Constructor_ValidateButtonText()
        {
            var buttons = new string[4] {"b1", "b2", "b3", "b4"};
            var vm = new MultiChoiceMsgBoxViewModel("title", "errorText", buttons); 
            Assert.Matches(buttons[0], vm.Button1Text);
            Assert.Matches(buttons[1], vm.Button2Text);
            Assert.Matches(buttons[2], vm.Button3Text);
            Assert.Matches(buttons[3], vm.Button4Text);

            buttons = new string[2] {"b1", "b2"};
            vm = new MultiChoiceMsgBoxViewModel("title", "errorText", buttons); 
            Assert.Matches(buttons[0], vm.Button1Text);
            Assert.Matches(buttons[1], vm.Button2Text);
            Assert.Null(vm.Button3Text);
            Assert.Null(vm.Button4Text);
        }

        [Fact]
        public void Constructor_ValidateErrorMsgText()
        {
            var buttons = new string[1] {"b1"};
            var vm = new MultiChoiceMsgBoxViewModel("title", "errorText", buttons); 
            Assert.Matches("errorText", vm.ErrorMsgText);

        }

        [Fact]
        public void Constructor_ValidateDialogTitleText()
        {
            var buttons = new string[1] {"b1"};
            var vm = new MultiChoiceMsgBoxViewModel("title", "errorText", buttons); 
            Assert.Matches("title", vm.DialogTitle);

        }

        [Fact]
        public void ButtonClickCommand_Validate()
        {
            var buttons = new string[4] {"b1", "b2", "b3", "b4"};
            var vm = new MultiChoiceMsgBoxViewModel("title", "errorText", buttons); 
            
            MultiChoiceMsgBoxResult result = MultiChoiceMsgBoxResult.Cancel;
            vm.CloseDialog += (s, e) => { result = e;};
            Assert.True(vm.ButtonClickCommand.CanExecute(null));

            vm.ButtonClickCommand.Execute(MultiChoiceMsgBoxResult.Button3);

            Assert.True(MultiChoiceMsgBoxResult.Button3 == result);
        }
    }
}
