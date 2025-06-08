#pragma warning disable CA1515

namespace TaskbarToolsDemo;

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TaskbarTools;

/// <summary>
/// Represents an object that tests Taskbar features.
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
{
    #region Init
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        CurrentStateTextInternal = "Initializing...";
        TestTimer = new Timer(new TimerCallback(TestTimerCallback));
        Loaded += OnLoaded;

        TestTimerDelegate = OnTestTimerStep1;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CurrentStateText = "Loaded, please wait...";

        MainIcon = LoadResourceIcon("Idle-Enabled.ico");
        MoonIcon = LoadResourceIcon("moon.ico");
        Menu = (ContextMenu)FindResource("Menu");
        CloseBitmap = LoadResourceBitmap("UAC-16.png");
        CommandClose = (ICommand)FindResource("CommandClose");

        _ = TestTimer?.Change(TimeSpan.FromSeconds(0), Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Gets the current state text of the application, which is displayed in the main window.
    /// </summary>
    public string CurrentStateText
    {
        get => CurrentStateTextInternal;
        private set
        {
            if (CurrentStateTextInternal != value)
            {
                CurrentStateTextInternal = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentStateText)));
            }
        }
    }

    private string CurrentStateTextInternal;
    #endregion

    #region Timers
    private void TestTimerCallback(object? parameter) => Dispatcher.Invoke(TestTimerDelegate);

    private void OnTestTimerStep1()
    {
        CurrentStateText = "Step 1, please wait...";

        AppTaskbarIcon = TaskbarIcon.Create(MainIcon, null, null, null);

        TestTimerDelegate = OnTestTimerStep2;
        _ = TestTimer?.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
    }

    private void OnTestTimerStep2()
    {
        CurrentStateText = "Step 2, please wait...";

        AppTaskbarIcon?.Dispose();
        AppTaskbarIcon = null;

        TestTimerDelegate = OnTestTimerStep3;
        _ = TestTimer?.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
    }

    private void OnTestTimerStep3()
    {
        CurrentStateText = "Last step done.";

        AppTaskbarIcon = TaskbarIcon.Create(MainIcon, "test", Menu, this);

        _ = TestTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        TestTimer?.Dispose();
        TestTimer = null;
    }
    #endregion

    #region Events
    private void OnClose(object sender, ExecutedRoutedEventArgs e)
    {
        AppTaskbarIcon?.Dispose();
        AppTaskbarIcon = null;

        Close();
    }

    private void OnClearToolTip(object sender, ExecutedRoutedEventArgs e) => AppTaskbarIcon?.UpdateToolTipText(null);

    private void OnSetToolTip(object sender, ExecutedRoutedEventArgs e) => AppTaskbarIcon?.UpdateToolTipText("New tooltip");

    private void OnSetIcon(object sender, ExecutedRoutedEventArgs e) => AppTaskbarIcon?.UpdateIcon(MoonIcon);

    private void OnClearCloseIcon(object sender, ExecutedRoutedEventArgs e)
    {
        const Bitmap? NullBitmap = null;
        TaskbarIcon.SetMenuIcon(CommandClose, NullBitmap);
    }

    private void OnSetCloseIcon(object sender, ExecutedRoutedEventArgs e) => TaskbarIcon.SetMenuIcon(CommandClose, CloseBitmap);

    private void OnEnable(object sender, ExecutedRoutedEventArgs e) => TaskbarIcon.SetMenuIsEnabled(CommandClose, true);

    private void OnDisable(object sender, ExecutedRoutedEventArgs e) => TaskbarIcon.SetMenuIsEnabled(CommandClose, false);

    private void OnShowBalloon(object sender, ExecutedRoutedEventArgs e) => TaskbarBalloon.Show("Balloon Text", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

    private void OnChangeText(object sender, ExecutedRoutedEventArgs e) => TaskbarIcon.SetMenuText(CommandClose, "New close text");

    private void OnShow(object sender, ExecutedRoutedEventArgs e) => TaskbarIcon.SetMenuIsVisible(CommandClose, true);

    private void OnHide(object sender, ExecutedRoutedEventArgs e) => TaskbarIcon.SetMenuIsVisible(CommandClose, false);
    #endregion

    #region Implementation
    private static Icon LoadResourceIcon(string resourceName)
    {
        Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
        using Stream ResourceStream = CurrentAssembly.GetManifestResourceStream($"TaskbarToolsDemo.{resourceName}")!;
        Icon ResourceIcon = new(ResourceStream);
        return ResourceIcon;
    }

    private static Bitmap LoadResourceBitmap(string resourceName)
    {
        Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
        using Stream ResourceStream = CurrentAssembly.GetManifestResourceStream($"TaskbarToolsDemo.{resourceName}")!;
        Bitmap ResourceBitmap = new(ResourceStream);
        return ResourceBitmap;
    }

    private Icon MainIcon = null!;
    private Icon MoonIcon = null!;
    private Bitmap CloseBitmap = null!;
    private ContextMenu Menu = null!;
    private ICommand CommandClose = null!;
    private TaskbarIcon? AppTaskbarIcon = TaskbarIcon.Empty;
    private Timer? TestTimer;
    private Action TestTimerDelegate;
    #endregion

    #region Implementation of INotifyPropertyChanged

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
    #endregion

    #region Implementation of IDisposable

    /// <summary>
    /// Called when an object should release its resources.
    /// </summary>
    /// <param name="isDisposing">Indicates if resources must be disposed now.</param>
    protected virtual void Dispose(bool isDisposing)
    {
        if (!IsDisposed)
        {
            IsDisposed = true;

            if (isDisposing)
                DisposeNow();
        }
    }

    /// <summary>
    /// Called when an object should release its resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="MainWindow"/> class.
    /// </summary>
    ~MainWindow()
    {
        Dispose(false);
    }

    /// <summary>
    /// True after <see cref="Dispose(bool)"/> has been invoked.
    /// </summary>
    private bool IsDisposed;

    /// <summary>
    /// Disposes of every reference that must be cleaned up.
    /// </summary>
    private void DisposeNow()
    {
        AppTaskbarIcon?.Dispose();
        CloseBitmap.Dispose();
        MainIcon.Dispose();
        MoonIcon.Dispose();
        TestTimer?.Dispose();
    }
    #endregion
}
