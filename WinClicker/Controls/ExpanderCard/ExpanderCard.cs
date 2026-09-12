using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinClicker.Controls
{
    public sealed partial class ExpanderCard : Control
    {
        public ExpanderCard()
        {
            DefaultStyleKey = typeof(ExpanderCard);
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(object), typeof(ExpanderCard), new PropertyMetadata(null));

        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty ActionButtonProperty =
            DependencyProperty.Register(nameof(ActionButton), typeof(object), typeof(ExpanderCard), new PropertyMetadata(null));

        public object ActionButton
        {
            get => GetValue(ActionButtonProperty);
            set => SetValue(ActionButtonProperty, value);
        }
    }
}
