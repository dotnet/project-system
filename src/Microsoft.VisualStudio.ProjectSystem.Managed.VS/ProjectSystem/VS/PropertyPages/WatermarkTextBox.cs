//Ported from "vset\qtools\testmanagement\ui\controls\wpf\controls" as this follows the simplest option so far
//Some unused properties are removed such as "Hitting Enter" support, etc.

using System;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// This text box functions like any other text box, except that it allows the consumer
    /// to specify a 'water mark' that appears in the text area (or somet other indicator
    /// based on the presence of text in the text property.
    /// 
    /// Additionally, it adds:
    /// HasInputtedText
    /// </summary>
    public class WatermarkTextBox : TextBox
    {
        const string c_WatermarkPropertyName = "Watermark";
        const string c_HasInputtedTextPropertyName = "HasInputtedText";
        const string c_WatermarkVerticalAlignmentPropertyName = "WatermarkVerticalAlignment";

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
            c_WatermarkVerticalAlignmentPropertyName,
            typeof(VerticalAlignment),
            typeof(WatermarkTextBox),
            new PropertyMetadata(VerticalAlignment.Center));

        public VerticalAlignment WatermarkVerticalAlignment
        {
            get { return (VerticalAlignment)this.GetValue(WatermarkVerticalAlignmentProperty); }
            set { this.SetValue(WatermarkVerticalAlignmentProperty, value); }
        }

        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            c_WatermarkPropertyName,
            typeof(string),
            typeof(WatermarkTextBox),
            new PropertyMetadata(String.Empty));

        /// <summary>
        /// The text to be displayed in the watermark area when there is no text/has no focus
        /// </summary>
        [Localizability(LocalizationCategory.Label, Modifiability = Modifiability.Modifiable, Readability = Readability.Readable)]
        public string Watermark
        {
            get { return this.GetValue(WatermarkProperty) as string; }
            set { this.SetValue(WatermarkProperty, value); }
        }

        static readonly DependencyPropertyKey HasInputtedTextPropertyKey = DependencyProperty.RegisterReadOnly(
            c_HasInputtedTextPropertyName,
            typeof(bool),
            typeof(WatermarkTextBox),
            new PropertyMetadata(false));

        public static readonly DependencyProperty HasInputtedTextProperty = HasInputtedTextPropertyKey.DependencyProperty;

        /// <summary>
        /// If there is text, that has been inputted, then this value will change to indicate that
        /// this control does indeed contain valid text.
        /// 
        /// Note that this is needed because the Text property changes all the time, and doesn't/cannot
        /// be reverted with styles to empty or null.
        /// </summary>
        public bool HasInputtedText
        {
            get { return (bool)this.GetValue(HasInputtedTextProperty); }
        }

        /// <summary>
        /// Called when the TextProperty Changes. Facilitates the HasInputtedText property changing
        /// as and when the control has no text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void TextPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            WatermarkTextBox source = sender as WatermarkTextBox;
            if (source == null)
            {
                return;
            }

            bool hasInputtedText = !String.IsNullOrEmpty(source.Text.Trim());
            if (hasInputtedText != source.HasInputtedText)
            {
                source.SetValue(HasInputtedTextPropertyKey, hasInputtedText);
            }
        }
    }
}