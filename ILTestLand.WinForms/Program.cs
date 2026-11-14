using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ILTestLand.WinForms;

// Headless WinForms target for obfuscation checks.
internal static class Program {
    private static int _failures;
    private static int _checks;

    [STAThread]
    private static int Main() {
        Application.EnableVisualStyles();

        using var form = new Form { Text = "sharphide-winforms" };
        _ = form.Handle;

        Section("form");
        Check("form.text", form.Text, "sharphide-winforms");
        Check("form.handle", form.IsHandleCreated, true);

        Section("events");
        var clicked = 0;
        var button = new Button { Text = "go" };
        button.Click += (_, _) => clicked++;
        form.Controls.Add(button);
        RaiseClick(button);
        RaiseClick(button);
        Check("event.click", clicked, 2);

        var keyModel = 0;
        var box = new TextBox();
        box.TextChanged += (_, _) => keyModel++;
        form.Controls.Add(box);
        box.Text = "typed";
        Check("event.textchanged", keyModel, 1);

        Section("data-binding");
        // Binding resolves public member names through reflection.
        var person = new PersonModel { Name = "ada", Age = 30 };
        var descriptors = TypeDescriptor.GetProperties(person);
        var nameDescriptor = descriptors[nameof(PersonModel.Name)]!;
        Check("bind.propdesc.get", nameDescriptor.GetValue(person), "ada");
        nameDescriptor.SetValue(person, "grace");
        Check("bind.propdesc.set", person.Name, "grace");
        Check("bind.propdesc.count", descriptors.Count >= 2, true);

        var roster = new BindingList<PersonModel> {
            new() { Name = "x", Age = 1 },
            new() { Name = "y", Age = 2 },
        };
        var source = new BindingSource { DataSource = roster };
        Check("bindingsource.count", source.Count, 2);
        source.Position = 1;
        Check("bindingsource.current", ((PersonModel)source.Current!).Name, "y");
        Check("bind.listitemprops",
            ListBindingHelper.GetListItemProperties(roster)[nameof(PersonModel.Age)] is not null, true);

        Section("bindinglist");
        var people = new BindingList<PersonModel>();
        var changes = 0;
        people.ListChanged += (_, _) => changes++;
        people.Add(new PersonModel { Name = "x", Age = 1 });
        people.Add(new PersonModel { Name = "y", Age = 2 });
        Check("bindinglist.count", people.Count, 2);
        Check("bindinglist.notify", changes, 2);
        people[0].Age = 99;
        Check("bindinglist.item-notify", changes >= 3, true);

        Section("list-controls");
        var combo = new ComboBox { DataSource = new[] { "red", "green", "blue" } };
        form.Controls.Add(combo);
        _ = combo.Handle;
        combo.SelectedIndex = 2;
        Check("combo.selecteditem", combo.SelectedItem, "blue");

        var listBox = new ListBox();
        listBox.Items.AddRange(["a", "b", "c"]);
        form.Controls.Add(listBox);
        Check("listbox.count", listBox.Items.Count, 3);

        Section("controls");
        var check = new CheckBox { Checked = false };
        check.Checked = true;
        Check("checkbox.checked", check.Checked, true);

        var numeric = new NumericUpDown { Minimum = 0, Maximum = 10, Value = 5 };
        numeric.Value = 8;
        Check("numericupdown.value", numeric.Value, 8m);

        var progress = new ProgressBar { Minimum = 0, Maximum = 100, Value = 40 };
        Check("progressbar.value", progress.Value, 40);

        var track = new TrackBar { Minimum = 0, Maximum = 10, Value = 7 };
        Check("trackbar.value", track.Value, 7);

        var label = new Label { AutoSize = true, Text = "measured" };
        form.Controls.Add(label);
        Check("label.autosize", label.PreferredSize.Width > 0, true);

        Section("layout");
        var table = new TableLayoutPanel { RowCount = 2, ColumnCount = 2 };
        table.Controls.Add(new Label { Text = "a" });
        table.Controls.Add(new Label { Text = "b" });
        form.Controls.Add(table);
        Check("tablelayout.controls", table.Controls.Count, 2);

        var flow = new FlowLayoutPanel();
        flow.Controls.Add(new Button());
        flow.Controls.Add(new Button());
        flow.Controls.Add(new Button());
        form.Controls.Add(flow);
        Check("flowlayout.controls", flow.Controls.Count, 3);

        var panel = new Panel();
        panel.Controls.Add(new TextBox { Name = "inner" });
        form.Controls.Add(panel);
        Check("controls.find", panel.Controls.Find("inner", true).Length, 1);

        Section("appearance");
        var colored = new Label { ForeColor = Color.FromArgb(0x33, 0x66, 0xFF) };
        Check("color.argb", colored.ForeColor.B, (byte)0xFF);

        using var font = new Font("Segoe UI", 12f, FontStyle.Bold);
        Check("font.bold", font.Bold, true);

        var tip = new ToolTip();
        tip.SetToolTip(button, "hint");
        Check("tooltip.get", tip.GetToolTip(button), "hint");

        Section("grid");
        var grid = new DataGridView { AutoGenerateColumns = true, AllowUserToAddRows = false, DataSource = people };
        form.Controls.Add(grid);
        _ = grid.Handle;
        Check("grid.rows", grid.RowCount, people.Count);
        Check("grid.cell", grid.Rows[0].Cells[nameof(PersonModel.Name)].Value, "x");

        Console.WriteLine(_failures == 0
            ? $"ALL PASS ({_checks} checks)"
            : $"{_failures}/{_checks} FAILURE(S)");
        return _failures;
    }

    // PerformClick needs a visible form; invoke OnClick directly.
    private static void RaiseClick(Control control) =>
        typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, [EventArgs.Empty]);

    private static void Section(string name) => Console.WriteLine($"-- {name} --");

    private static void Check<T>(string name, T actual, T expected) {
        _checks++;
        var ok = EqualityComparer<T>.Default.Equals(actual, expected);
        if (!ok)
            _failures++;
        Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {name} => {actual}");
    }
}

internal sealed class PersonModel : INotifyPropertyChanged {
    private string _name = string.Empty;
    private int _age;

    public string Name {
        get => _name;
        set {
            _name = value;
            Raise(nameof(Name));
        }
    }

    public int Age {
        get => _age;
        set {
            _age = value;
            Raise(nameof(Age));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
