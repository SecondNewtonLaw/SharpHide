using System.Windows;

namespace ILTestLand.Wpf;

public partial class TestWindow : Window {
    public int Clicks;

    public TestWindow() => InitializeComponent();

    private void OnIncrement(object sender, RoutedEventArgs e) {
        Clicks++;
        if (DataContext is PersonViewModel viewModel)
            viewModel.Count++;
    }
}
