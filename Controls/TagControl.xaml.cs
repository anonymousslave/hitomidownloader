using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace hitomiDownloader
{
    /// <summary>
    /// TagControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TagControl : ItemsControl
    {
        public static DependencyProperty TagCommandProperty = DependencyProperty.Register(nameof(TagCommand), typeof(ICommand), typeof(TagControl));
        public ICommand TagCommand
        {
            get => (ICommand)GetValue(TagCommandProperty);
            set => SetValue(TagCommandProperty, value);
        }
        public TagControl()
        {
            InitializeComponent();
        }
    }
}
