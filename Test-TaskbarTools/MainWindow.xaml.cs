namespace TestTaskbarTools
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using TaskbarTools;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            TestTimer = new Timer(new TimerCallback(TestTimerCallback));
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            MainIcon = LoadResourceIcon("Idle-Enabled.ico");
            MoonIcon = LoadResourceIcon("moon.ico");
            Menu = (ContextMenu)FindResource("Menu");
            CloseBitmap = LoadResourceBitmap("UAC-16.png");
            CommandClose = (ICommand)FindResource("CommandClose");

            TestTimerDelegate = OnTestTimerStep1;
            TestTimer.Change(TimeSpan.FromSeconds(0), Timeout.InfiniteTimeSpan);
        }

        private void TestTimerCallback(object parameter)
        {
            Dispatcher.Invoke(TestTimerDelegate);
        }

        private void OnTestTimerStep1()
        {
            AppTaskbarIcon = TaskbarIcon.Create(MainIcon, null, null, null);

            TestTimerDelegate = OnTestTimerStep2;
            TestTimer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
        }

        private void OnTestTimerStep2()
        {
            using (AppTaskbarIcon)
            {
            }

            TestTimerDelegate = OnTestTimerStep3;
            TestTimer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
        }

        private void OnTestTimerStep3()
        {
            AppTaskbarIcon = TaskbarIcon.Create(MainIcon, "test", Menu, this);

            TestTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            using (TestTimer)
            {
            }
        }

        private void OnClose(object sender, ExecutedRoutedEventArgs e)
        {
            using (AppTaskbarIcon)
            {
            }

            Close();
        }

        private void OnClearToolTip(object sender, ExecutedRoutedEventArgs e)
        {
            AppTaskbarIcon.UpdateToolTipText(null);
        }

        private void OnSetToolTip(object sender, ExecutedRoutedEventArgs e)
        {
            AppTaskbarIcon.UpdateToolTipText("New tooltip");
        }

        private void OnSetIcon(object sender, ExecutedRoutedEventArgs e)
        {
            AppTaskbarIcon.UpdateIcon(MoonIcon);
        }

        private void OnClearCloseIcon(object sender, ExecutedRoutedEventArgs e)
        {
            Bitmap NullBitmap = null;
            TaskbarIcon.SetMenuIcon(CommandClose, NullBitmap);
        }

        private void OnSetCloseIcon(object sender, ExecutedRoutedEventArgs e)
        {
            TaskbarIcon.SetMenuIcon(CommandClose, CloseBitmap);
        }

        private void OnEnable(object sender, ExecutedRoutedEventArgs e)
        {
            TaskbarIcon.SetMenuIsEnabled(CommandClose, true);
        }

        private void OnDisable(object sender, ExecutedRoutedEventArgs e)
        {
            TaskbarIcon.SetMenuIsEnabled(CommandClose, false);
        }

        private void OnChangeText(object sender, ExecutedRoutedEventArgs e)
        {
            TaskbarIcon.SetMenuText(CommandClose, "New close text");
        }

        private void OnShow(object sender, ExecutedRoutedEventArgs e)
        {
            TaskbarIcon.SetMenuIsVisible(CommandClose, true);
        }

        private void OnHide(object sender, ExecutedRoutedEventArgs e)
        {
            TaskbarIcon.SetMenuIsVisible(CommandClose, false);
        }

        private Icon LoadResourceIcon(string resourceName)
        {
            Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
            using (Stream ResourceStream = CurrentAssembly.GetManifestResourceStream($"TestTaskbarTools.{resourceName}"))
            {
                Icon ResourceIcon = new Icon(ResourceStream);
                return ResourceIcon;
            }
        }

        private Bitmap LoadResourceBitmap(string resourceName)
        {
            Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
            using (Stream ResourceStream = CurrentAssembly.GetManifestResourceStream($"TestTaskbarTools.{resourceName}"))
            {
                Bitmap ResourceBitmap = new Bitmap(ResourceStream);
                return ResourceBitmap;
            }
        }

        private Icon MainIcon;
        private Icon MoonIcon;
        private Bitmap CloseBitmap;
        private ContextMenu Menu;
        private ICommand CommandClose;
        private TaskbarIcon AppTaskbarIcon;
        private Timer TestTimer;
        private Action TestTimerDelegate;
    }
}
