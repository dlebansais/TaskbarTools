namespace TaskbarIconHost
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Forms;
    using Microsoft.Win32;

    /// <summary>
    /// Class that provide some information about the Windows taskbar.
    /// </summary>
    public static class TaskbarLocation
    {
        #region Init
        static TaskbarLocation()
        {
            UpdateLocation();

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        private static void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            UpdateLocation();
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Desktop)
                UpdateLocation();
        }

        private static void UpdateLocation()
        {
            TaskbarHandle = IntPtr.Zero;
            CurrentScreen = null;

            IntPtr hwnd;
            if (GetSystemTrayHandle(out hwnd))
            {
                TaskbarHandle = hwnd;

                NativeMethods.RECT TrayRect, NotificationAreaRect, IconAreaRect;
                if (GetSystemTrayRect(out TrayRect, out NotificationAreaRect, out IconAreaRect))
                {
                    System.Drawing.Rectangle TrayDrawingRect = new System.Drawing.Rectangle(TrayRect.Left, TrayRect.Top, TrayRect.Right - TrayRect.Left, TrayRect.Bottom - TrayRect.Top);
                    Dictionary<Screen, int> AreaTable = new Dictionary<Screen, int>();

                    foreach (Screen s in Screen.AllScreens)
                    {
                        System.Drawing.Rectangle ScreenDrawingRect = s.Bounds;
                        ScreenDrawingRect.Intersect(TrayDrawingRect);
                        int IntersectionArea = ScreenDrawingRect.Width * ScreenDrawingRect.Height;

                        AreaTable.Add(s, IntersectionArea);
                    }

                    Screen? SelectedScreen = null;
                    int SmallestPositiveArea = 0;

                    foreach (KeyValuePair<Screen, int> Entry in AreaTable)
                    {
                        if (SelectedScreen == null || (Entry.Value > 0 && (SmallestPositiveArea == 0 || SmallestPositiveArea > Entry.Value)))
                        {
                            SelectedScreen = Entry.Key;
                            SmallestPositiveArea = Entry.Value;
                        }
                    }

                    CurrentScreen = SelectedScreen;
                }
            }
        }

        private static IntPtr TaskbarHandle;
        private static Screen? CurrentScreen;
        #endregion

        #region User Interface
        /// <summary>
        /// Gets the bounds of the current screen used to display the taskbar.
        /// </summary>
        public static System.Drawing.Rectangle ScreenBounds
        {
            get { return CurrentScreen == null ? System.Drawing.Rectangle.Empty : CurrentScreen.Bounds; }
        }

        /// <summary>
        /// Return the position a FrameworkElement should take to be on the edge of the task bar. In screen coordinates.
        /// </summary>
        /// <param name="element">The element for which the position should be calculated.</param>
        /// <returns>The position <paramref name="element"/> should be at to be on the side where the taskbar is.</returns>
        public static Point GetRelativePosition(FrameworkElement element)
        {
            if (element == null || double.IsNaN(element.ActualWidth) || double.IsNaN(element.ActualHeight) || ScreenBounds.IsEmpty)
                return new Point(double.NaN, double.NaN);

            System.Drawing.Point FormsMousePosition = Control.MousePosition;
            Point MousePosition = new Point(FormsMousePosition.X, FormsMousePosition.Y);

            Rect WorkArea = SystemParameters.WorkArea;

            double WorkScreenWidth = WorkArea.Right - WorkArea.Left;
            double WorkScreenHeight = WorkArea.Bottom - WorkArea.Top;
            double CurrentScreenWidth = ScreenBounds.Right - ScreenBounds.Left;
            double CurrentScreenHeight = ScreenBounds.Bottom - ScreenBounds.Top;

            double RatioX = WorkScreenWidth / CurrentScreenWidth;
            double RatioY = WorkScreenHeight / CurrentScreenHeight;

            Size PopupSize = new Size((int)(element.ActualWidth / RatioX), (int)(element.ActualHeight / RatioY));
            Point RelativePosition = TaskbarLocation.GetRelativePosition(MousePosition, PopupSize);

            RelativePosition = new Point(RelativePosition.X * RatioX, RelativePosition.Y * RatioY);

            return RelativePosition;
        }

        // From a position, and a window size, all in screen coordinates, return the position the window should take
        // to be on the edge of the task bar. In screen coordinates.
        private static Point GetRelativePosition(Point position, Size size)
        {
            NativeMethods.RECT TrayRect, NotificationAreaRect, IconAreaRect;
            if (CurrentScreen == null || !GetSystemTrayRect(out TrayRect, out NotificationAreaRect, out IconAreaRect))
                return new Point(0, 0);

            // Use the full taskbar rectangle.
            NativeMethods.RECT TaskbarRect = TrayRect;

            double X;
            double Y;

            // If the potion isn't within the taskbar (shouldn't happen), default to bottom.
            if (!(position.X >= TaskbarRect.Left && position.X < TaskbarRect.Right && position.Y >= TaskbarRect.Top && position.Y < TaskbarRect.Bottom))
                AlignedToBottom(position, size, TaskbarRect, out X, out Y);
            else
            {
                // Otherwise, check where the taskbar is, and calculate an aligned position.
                switch (GetTaskBarLocation(TaskbarRect))
                {
                    case TaskBarLocation.Top:
                        AlignedToTop(position, size, TaskbarRect, out X, out Y);
                        break;

                    default:
                    case TaskBarLocation.Bottom:
                        AlignedToBottom(position, size, TaskbarRect, out X, out Y);
                        break;

                    case TaskBarLocation.Left:
                        AlignedToLeft(position, size, TaskbarRect, out X, out Y);
                        break;

                    case TaskBarLocation.Right:
                        AlignedToRight(position, size, TaskbarRect, out X, out Y);
                        break;
                }
            }

            return new Point(X, Y);
        }
        #endregion

        #region Implementation
        private enum TaskBarLocation
        {
            Top,
            Bottom,
            Left,
            Right,
        }

        private static TaskBarLocation GetTaskBarLocation(NativeMethods.RECT taskbarRect)
        {
            Point TaskbarCenter = new Point((taskbarRect.Left + taskbarRect.Right) / 2, (taskbarRect.Top + taskbarRect.Bottom) / 2);

            bool IsTop = TaskbarCenter.Y < CurrentScreen?.WorkingArea.Top + ((CurrentScreen?.WorkingArea.Bottom - CurrentScreen?.WorkingArea.Top) / 4);
            bool IsBottom = TaskbarCenter.Y >= CurrentScreen?.WorkingArea.Bottom - ((CurrentScreen?.WorkingArea.Bottom - CurrentScreen?.WorkingArea.Top) / 4);
            bool IsLeft = TaskbarCenter.X < CurrentScreen?.WorkingArea.Left + ((CurrentScreen?.WorkingArea.Right - CurrentScreen?.WorkingArea.Left) / 4);
            bool IsRight = TaskbarCenter.X >= CurrentScreen?.WorkingArea.Right - ((CurrentScreen?.WorkingArea.Right - CurrentScreen?.WorkingArea.Left) / 4);

            if (IsTop && !IsLeft && !IsRight)
                return TaskBarLocation.Top;
            else if (IsBottom && !IsLeft && !IsRight)
                return TaskBarLocation.Bottom;
            else if (IsLeft && !IsTop && !IsBottom)
                return TaskBarLocation.Left;
            else if (IsRight && !IsTop && !IsBottom)
                return TaskBarLocation.Right;
            else
                return TaskBarLocation.Bottom;
        }

        private static void AlignedToLeft(Point position, Size size, NativeMethods.RECT taskbarRect, out double x, out double y)
        {
            x = taskbarRect.Right;
            y = position.Y - (size.Height / 2);
        }

        private static void AlignedToRight(Point position, Size size, NativeMethods.RECT taskbarRect, out double x, out double y)
        {
            x = taskbarRect.Left - size.Width;
            y = position.Y - (size.Height / 2);
        }

        private static void AlignedToTop(Point position, Size size, NativeMethods.RECT taskbarRect, out double x, out double y)
        {
            x = position.X - (size.Width / 2);
            y = taskbarRect.Bottom;
        }

        private static void AlignedToBottom(Point position, Size size, NativeMethods.RECT taskbarRect, out double x, out double y)
        {
            x = position.X - (size.Width / 2);
            y = taskbarRect.Top - size.Height;
        }

        private static bool GetSystemTrayHandle(out IntPtr hwnd)
        {
            hwnd = IntPtr.Zero;

            IntPtr hWndTray = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (hWndTray != IntPtr.Zero)
            {
                hwnd = hWndTray;

                hWndTray = NativeMethods.FindWindowEx(hWndTray, IntPtr.Zero, "TrayNotifyWnd", null);
                if (hWndTray != IntPtr.Zero)
                {
                    hWndTray = NativeMethods.FindWindowEx(hWndTray, IntPtr.Zero, "SysPager", null);
                    if (hWndTray != IntPtr.Zero)
                    {
                        hWndTray = NativeMethods.FindWindowEx(hWndTray, IntPtr.Zero, "ToolbarWindow32", null);
                        if (hWndTray != IntPtr.Zero)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool GetSystemTrayRect(out NativeMethods.RECT trayRect, out NativeMethods.RECT notificationAreaRect, out NativeMethods.RECT iconAreaRect)
        {
            trayRect = new NativeMethods.RECT() { Left = 0, Top = 0, Right = 0, Bottom = 0 };
            notificationAreaRect = new NativeMethods.RECT() { Left = 0, Top = 0, Right = 0, Bottom = 0 };
            iconAreaRect = new NativeMethods.RECT() { Left = 0, Top = 0, Right = 0, Bottom = 0 };

            IntPtr hWndTray = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (hWndTray != IntPtr.Zero)
            {
                NativeMethods.GetWindowRect(hWndTray, ref trayRect);

                hWndTray = NativeMethods.FindWindowEx(hWndTray, IntPtr.Zero, "TrayNotifyWnd", null);
                if (hWndTray != IntPtr.Zero)
                {
                    NativeMethods.GetWindowRect(hWndTray, ref notificationAreaRect);

                    hWndTray = NativeMethods.FindWindowEx(hWndTray, IntPtr.Zero, "SysPager", null);
                    if (hWndTray != IntPtr.Zero)
                    {
                        hWndTray = NativeMethods.FindWindowEx(hWndTray, IntPtr.Zero, "ToolbarWindow32", null);
                        if (hWndTray != IntPtr.Zero)
                        {
                            NativeMethods.GetWindowRect(hWndTray, ref iconAreaRect);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool ToScreen(ref Point position)
        {
            NativeMethods.POINT p1 = new NativeMethods.POINT() { X = 0, Y = 0 };
            NativeMethods.POINT p2 = new NativeMethods.POINT() { X = 1000, Y = 1000 };

            if (TaskbarHandle != IntPtr.Zero && NativeMethods.ClientToScreen(TaskbarHandle, ref p1) && NativeMethods.ClientToScreen(TaskbarHandle, ref p2))
            {
                double RatioX = (double)(p2.X - p1.X) / 1000;
                double RatioY = (double)(p2.Y - p1.Y) / 1000;

                position = new Point(position.X * RatioX, position.Y * RatioY);
                return true;
            }

            return false;
        }

        private static bool ToScreen(ref Size size)
        {
            NativeMethods.POINT p1 = new NativeMethods.POINT() { X = 0, Y = 0 };
            NativeMethods.POINT p2 = new NativeMethods.POINT() { X = 1000, Y = 1000 };

            if (TaskbarHandle != IntPtr.Zero && NativeMethods.ClientToScreen(TaskbarHandle, ref p1) && NativeMethods.ClientToScreen(TaskbarHandle, ref p2))
            {
                double RatioX = (double)(p2.X - p1.X) / 1000;
                double RatioY = (double)(p2.Y - p1.Y) / 1000;

                size = new Size(size.Width * RatioX, size.Height * RatioY);
                return true;
            }

            return false;
        }

        private static bool ToClient(ref Point position)
        {
            NativeMethods.POINT p1 = new NativeMethods.POINT() { X = 0, Y = 0 };
            NativeMethods.POINT p2 = new NativeMethods.POINT() { X = 1000, Y = 1000 };

            if (TaskbarHandle != IntPtr.Zero && NativeMethods.ScreenToClient(TaskbarHandle, ref p1) && NativeMethods.ScreenToClient(TaskbarHandle, ref p2))
            {
                double RatioX = (double)(p2.X - p1.X) / 1000;
                double RatioY = (double)(p2.Y - p1.Y) / 1000;

                position = new Point(position.X * RatioX, position.Y * RatioY);
                return true;
            }

            return false;
        }

        private static bool ToClient(ref Size size)
        {
            NativeMethods.POINT p1 = new NativeMethods.POINT() { X = 0, Y = 0 };
            NativeMethods.POINT p2 = new NativeMethods.POINT() { X = 1000, Y = 1000 };

            if (TaskbarHandle != IntPtr.Zero && NativeMethods.ScreenToClient(TaskbarHandle, ref p1) && NativeMethods.ScreenToClient(TaskbarHandle, ref p2))
            {
                double RatioX = (double)(p2.X - p1.X) / 1000;
                double RatioY = (double)(p2.Y - p1.Y) / 1000;

                size = new Size(size.Width * RatioX, size.Height * RatioY);
                return true;
            }

            return false;
        }
        #endregion
    }
}
