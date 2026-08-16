---
layout: chapter
title: "Desktop User Interfaces"
number: 4
part: 2
---

## Winforms {#winforms}

Windows Forms is the desktop UI framework that has shipped with .NET since the beginning. It is still supported and still a reasonable choice for line of business applications on Windows, especially when a team already knows it. On modern .NET it is Windows only, so set the target framework accordingly.

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

Create a new project from the command line.

```bash
dotnet new winforms -o YourApp
cd YourApp
dotnet run
```

### A form with a control and an event handler

The designer generates most of this for you, but it is useful to see what it produces. A form is a class that inherits from `Form`, controls are fields on that class, and user interaction is handled by subscribing to events.

```cs
using System;
using System.Windows.Forms;

public class ShowForm : Form
{
    private readonly TextBox _showName = new TextBox { Left = 10, Top = 10, Width = 200 };
    private readonly Button _save = new Button { Left = 220, Top = 10, Text = "Save" };
    private readonly ListBox _shows = new ListBox { Left = 10, Top = 45, Width = 410, Height = 200 };

    public ShowForm()
    {
        Text = "TV Shows";
        ClientSize = new System.Drawing.Size(440, 260);
        Controls.AddRange(new Control[] { _showName, _save, _shows });

        _save.Click += Save_Click;
    }

    private void Save_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_showName.Text))
        {
            MessageBox.Show("Show name cannot be empty", "Validation");
            return;
        }

        _shows.Items.Add(_showName.Text);
        _showName.Clear();
    }
}
```

### Keep the UI thread responsive

Everything in the paragraphs above about async/await applies here, and it matters more in a desktop app than anywhere else. Any slow work performed directly in an event handler freezes the window, because the same thread that runs your handler also paints the form and processes input.

Mark the handler `async` and await the slow work. Winforms installs a synchronization context, so execution resumes on the UI thread after the await and it is safe to touch controls again. Do not use `ConfigureAwait(false)` in code that will go on to update the UI.

```cs
private async void Load_Click(object sender, EventArgs e)
{
    _load.Enabled = false;
    try
    {
        // runs off the UI thread, window stays responsive
        var shows = await _repo.GetShowsAsync();

        // back on the UI thread here
        _shows.Items.Clear();
        _shows.Items.AddRange(shows.ToArray());
    }
    finally
    {
        _load.Enabled = true;
    }
}
```

`async void` is normally something to avoid, but event handlers are the one place it is correct, because the event signature returns void. Wrap the body in a try/catch or an unhandled exception will take down the process.

If you have work running on a background thread that is not awaited, you cannot update controls from it directly. Marshal back to the UI thread with `Invoke`.

```cs
if (_shows.InvokeRequired)
{
    _shows.Invoke(() => _shows.Items.Add(name));
}
else
{
    _shows.Items.Add(name);
}
```

### Cross platform alternatives

Winforms does not run on linux or mac. If that matters:

- [Majorsilence.Forms](https://github.com/majorsilence/Majorsilence.Forms) - a Winforms compatibility layer, useful for porting an existing Winforms codebase.
- [Avalonia](https://avaloniaui.net/) - a mature cross platform XAML UI framework.
- [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/) - Microsoft's cross platform framework, see [Microsoft Maui](#microsoft-maui) below.

## Microsoft Maui {#microsoft-maui}

[.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/) (Multi-platform App UI) builds native desktop and mobile applications from a single c# codebase, targeting android, iOS, mac, and windows. It is the successor to Xamarin.Forms.

Unlike [Winforms](#winforms) above, which is windows only, MAUI renders through each platform's own native controls, so an app looks like an android app on android and a mac app on mac.

Install the workload and create a project.

```bash
dotnet workload install maui
dotnet new maui -o YourApp
cd YourApp

# run on a specific platform
dotnet build -t:Run -f net10.0-android
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

A MAUI project targets several frameworks at once from one csproj.

```xml
<PropertyGroup>
  <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
  <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">
    $(TargetFrameworks);net10.0-windows10.0.19041.0
  </TargetFrameworks>
  <OutputType>Exe</OutputType>
  <UseMaui>true</UseMaui>
  <SingleProject>true</SingleProject>
</PropertyGroup>
```

Building for iOS or mac requires a mac. Android and windows build anywhere.

### Pages and XAML

UI is normally written in XAML, with the logic in a matching code behind file.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="YourApp.ShowsPage"
             Title="TV Shows">
    <VerticalStackLayout Padding="20" Spacing="10">
        <Entry x:Name="ShowNameEntry" Placeholder="Show name" />
        <Button Text="Add" Clicked="OnAddClicked" />
        <CollectionView x:Name="ShowsList">
            <CollectionView.ItemTemplate>
                <DataTemplate>
                    <Label Text="{Binding ShowName}" FontSize="18" />
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
    </VerticalStackLayout>
</ContentPage>
```

```cs
public partial class ShowsPage : ContentPage
{
    private readonly ObservableCollection<TvShow> _shows = new();

    public ShowsPage()
    {
        InitializeComponent();
        ShowsList.ItemsSource = _shows;
    }

    private void OnAddClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ShowNameEntry.Text))
        {
            return;
        }

        _shows.Add(new TvShow { ShowName = ShowNameEntry.Text });
        ShowNameEntry.Text = "";
    }
}
```

`ObservableCollection<T>` is what makes the list update itself. It raises a change notification on add and remove, which the `CollectionView` listens for. A plain `List<T>` will not refresh the UI.

### Dependency injection

MAUI uses the same container described in the **IOC** section. Register services in `MauiProgram.cs` and pages resolve their dependencies through the constructor.

```cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddSingleton<ITestRepo>(sp =>
            new TestRepoNobase("Data Source=shows.db"));
        builder.Services.AddTransient<ShowsPage>();

        return builder.Build();
    }
}
```

### Keeping the UI responsive

The same rule as Winforms applies: slow work in a handler freezes the UI. Await it instead, and marshal back to the UI thread from any background work using `MainThread`.

```cs
private async void OnLoadClicked(object sender, EventArgs e)
{
    var shows = await _repo.GetShowsAsync();

    MainThread.BeginInvokeOnMainThread(() =>
    {
        _shows.Clear();
        foreach (var show in shows)
        {
            _shows.Add(show);
        }
    });
}
```

### Alternatives

- [Avalonia](https://avaloniaui.net/) - cross platform XAML UI, also runs on linux, which MAUI does not target.
- [Uno Platform](https://platform.uno/) - WinUI style markup across mobile, desktop, and WebAssembly.
