using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace ILTestLand.Wpf;

/*
 *  Gotta make sure basic XAML and WPF works. This shit will likely break in the future, so better test all of it.
 */

internal static class Program {
    private static int _failures;
    private static int _checks;

    [STAThread]
    private static int Main() {
        _ = new Application();

        var viewModel = new PersonViewModel { Name = "grace" };
        var window = new TestWindow { DataContext = viewModel };

        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(320, 240));
        root.Arrange(new Rect(0, 0, 320, 240));
        root.UpdateLayout();

        Section("xaml-load");
        Check("xaml.title", window.Title, "sharphide-wpf");
        Check("xaml.name.field", window.NameBox is not null, true);

        Section("binding");
        Pull(window.NameBox, TextBox.TextProperty);
        Pull(window.GreetingText, TextBlock.TextProperty);
        Pull(window.UpperText, TextBlock.TextProperty);
        Check("bind.oneway.name", window.NameBox!.Text, "grace");
        Check("bind.computed.greeting", window.GreetingText.Text, "hi grace");
        Check("bind.converter", window.UpperText.Text, "GRACE");
        Check("bind.collection", window.ItemsList.Items.Count, 3);

        window.NameBox!.Text = "ada";
        window.NameBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        Check("bind.twoway.push", viewModel.Name, "ada");
        Pull(window.GreetingText, TextBlock.TextProperty);
        Check("bind.notify.greeting", window.GreetingText.Text, "hi ada");

        Section("resources");
        Check("resource.brush", ((SolidColorBrush)window.AccentBorder.Background).Color,
            Color.FromRgb(0x33, 0x66, 0xCC));
        Check("resource.converter.lookup", window.FindResource("Upper") is UpperConverter, true);

        Section("events");
        window.IncButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, window.IncButton));
        Check("event.xaml.click", window.Clicks, 1);
        Check("event.click.vm", viewModel.Count, 1);

        Console.WriteLine(_failures == 0
            ? $"ALL PASS ({_checks} checks)"
            : $"{_failures}/{_checks} FAILURE(S)");
        return _failures;
    }

    private static void Pull(FrameworkElement? element, DependencyProperty property) =>
        element?.GetBindingExpression(property)?.UpdateTarget();

    private static void Section(string name) => Console.WriteLine($"-- {name} --");

    private static void Check<T>(string name, T actual, T expected) {
        _checks++;
        var ok = EqualityComparer<T>.Default.Equals(actual, expected);
        if (!ok)
            _failures++;
        Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {name} => {actual}");
    }
}

public sealed class PersonViewModel : INotifyPropertyChanged {
    private string _name = string.Empty;
    private int _count;

    public string Name {
        get => _name;
        set {
            _name = value;
            Raise(nameof(Name));
            Raise(nameof(Greeting));
        }
    }

    public string Greeting => $"hi {_name}";

    public int Count {
        get => _count;
        set {
            _count = value;
            Raise(nameof(Count));
        }
    }

    public ObservableCollection<string> Items { get; } = ["a", "b", "c"];

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}

internal sealed class UpperConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.ToUpperInvariant() ?? string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value ?? string.Empty;
}