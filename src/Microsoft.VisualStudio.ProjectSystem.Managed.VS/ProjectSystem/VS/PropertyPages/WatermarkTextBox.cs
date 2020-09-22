// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// This text box functions like any other text box, except that it allows the consumer
    /// to specify a 'water mark' that appears in the text area (or some other indicator
    /// based on the presence of text in the text property.
    ///
    /// Additionally, it adds:
    /// HasInputtedText
    /// </summary>
    internal class WatermarkTextBox : TextBox
    {
        private const string WatermarkPropertyName = "Watermark";
        private const string HasInputtedTextPropertyName = "HasInputtedText";
        private const string WatermarkVerticalAlignmentPropertyName = "WatermarkVerticalAlignment";

        /// <summary>
        /// Primarily, this static constructor will register the metadata overrides, for such things as
        /// resource look up, and property changing notifications.
        /// </summary>
        static WatermarkTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WatermarkTextBox), new FrameworkPropertyMetadata(typeof(WatermarkTextBox)));
            TextProperty.OverrideMetadata(typeof(WatermarkTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(TextPropertyChanged)));
        }

        public static readonly DependencyProperty WatermarkVerticalAlignmentProperty = DependencyProperty.Register(
            WatermarkVerticalAlignmentPropertyName,
            typeof(VerticalAlignment),
            typeof(WatermarkTextBox),
            new PropertyMetadata(VerticalAlignment.Center));

        public VerticalAlignment WatermarkVerticalAlignment
        {
            get { return (VerticalAlignment)GetValue(WatermarkVerticalAlignmentProperty); }
            set { SetValue(WatermarkVerticalAlignmentProperty, value); }
        }

        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            WatermarkPropertyName,
            typeof(string),
            typeof(WatermarkTextBox),
            new PropertyMetadata(string.Empty));

        /// <summary>
        /// The text to be displayed in the watermark area when there is no text/has no focus
        /// </summary>
        [Localizability(LocalizationCategory.Label, Modifiability = Modifiability.Modifiable, Readability = Readability.Readable)]
        public string Watermark
        {
            get { return (string)GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        private static readonly DependencyPropertyKey s_hasInputtedTextPropertyKey = DependencyProperty.RegisterReadOnly(
            HasInputtedTextPropertyName,
            typeof(bool),
            typeof(WatermarkTextBox),
            new PropertyMetadata(false));

        public static readonly DependencyProperty HasInputtedTextProperty = s_hasInputtedTextPropertyKey.DependencyProperty;

        /// <summary>
        /// If there is text, that has been inputted, then this value will change to indicate that
        /// this control does indeed contain valid text.
        ///
        /// Note that this is needed because the Text property changes all the time, and doesn't/cannot
        /// be reverted with styles to empty or null.
        /// </summary>
        public bool HasInputtedText
        {
            get { return (bool)GetValue(HasInputtedTextProperty); }
        }

        /// <summary>
        /// Called when the TextProperty Changes. Facilitates the HasInputtedText property changing
        /// as and when the control has no text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void TextPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is not WatermarkTextBox source)
            {
                return;
            }

            bool hasInputtedText = !string.IsNullOrEmpty(source.Text.Trim());
            if (hasInputtedText != source.HasInputtedText)
            {
                source.SetValue(s_hasInputtedTextPropertyKey, hasInputtedText);
            }
        }
    }
}
