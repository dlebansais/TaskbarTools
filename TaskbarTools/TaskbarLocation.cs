namespace TaskbarTools;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using Contracts;
using Microsoft.Win32;

/// <summary>
/// Class that provide some information about the Windows taskbar.
/// </summary>
public static partial class TaskbarLocation
{
    #region Init
    static TaskbarLocation()
    {
        UpdateLocation();

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private static void OnDisplaySettingsChanged(object? sender, EventArgs e) => UpdateLocation();

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.Desktop)
            UpdateLocation();
    }

    private static void UpdateLocation()
    {
        CurrentScreen = null;

        if (!GetSystemTrayRect(TaskbarAreas.TrayArea, out TaskbarRects Rects))
            return;

        NativeMethods.RECT TrayRect = Rects.TrayRect;
        System.Drawing.Rectangle TrayDrawingRect = new(TrayRect.Left, TrayRect.Top, TrayRect.Right - TrayRect.Left, TrayRect.Bottom - TrayRect.Top);
        Dictionary<Screen, int> AreaTable = [];

        foreach (Screen Screen in Screen.AllScreens)
        {
            System.Drawing.Rectangle ScreenDrawingRect = Screen.Bounds;
            ScreenDrawingRect.Intersect(TrayDrawingRect);
            int IntersectionArea = ScreenDrawingRect.Width * ScreenDrawingRect.Height;

            AreaTable.Add(Screen, IntersectionArea);
        }

        Screen? SelectedScreen = null;
        int SmallestPositiveArea = 0;

        foreach (KeyValuePair<Screen, int> Entry in AreaTable)
        {
            if (SelectedScreen is null || (Entry.Value > 0 && (SmallestPositiveArea == 0 || SmallestPositiveArea > Entry.Value)))
            {
                SelectedScreen = Entry.Key;
                SmallestPositiveArea = Entry.Value;
            }
        }

        CurrentScreen = SelectedScreen;
    }

    private static Screen? CurrentScreen;
    #endregion

    #region User Interface
    /// <summary>
    /// Gets the bounds of the current screen used to display the taskbar.
    /// </summary>
    public static System.Drawing.Rectangle ScreenBounds => CurrentScreen is null ? System.Drawing.Rectangle.Empty : CurrentScreen.Bounds;

    /// <summary>
    /// Returns the position a FrameworkElement should take to be on the edge of the task bar. In screen coordinates.
    /// </summary>
    /// <param name="element">The element for which the position should be calculated.</param>
    /// <returns>The position <paramref name="element"/> should be at to be on the side where the taskbar is.</returns>
    [RequireNotNull(nameof(element))]
    private static Point GetRelativePositionVerified(FrameworkElement element)
    {
        if (double.IsNaN(element.ActualWidth) || double.IsNaN(element.ActualHeight) || ScreenBounds.IsEmpty)
            return new Point(double.NaN, double.NaN);

        System.Drawing.Point FormsMousePosition = Control.MousePosition;
        Point MousePosition = new(FormsMousePosition.X, FormsMousePosition.Y);

        Rect WorkArea = SystemParameters.WorkArea;

        double WorkScreenWidth = WorkArea.Right - WorkArea.Left;
        double WorkScreenHeight = WorkArea.Bottom - WorkArea.Top;
        double CurrentScreenWidth = ScreenBounds.Right - ScreenBounds.Left;
        double CurrentScreenHeight = ScreenBounds.Bottom - ScreenBounds.Top;

        double RatioX = WorkScreenWidth / CurrentScreenWidth;
        double RatioY = WorkScreenHeight / CurrentScreenHeight;

        Size PopupSize = new((int)(element.ActualWidth / RatioX), (int)(element.ActualHeight / RatioY));
        Point RelativePosition = GetRelativePosition(MousePosition, PopupSize);

        RelativePosition = new Point(RelativePosition.X * RatioX, RelativePosition.Y * RatioY);

        return RelativePosition;
    }

    // From a position, and a window size, all in screen coordinates, return the position the window should take
    // to be on the edge of the task bar. In screen coordinates.
    private static Point GetRelativePosition(Point position, Size size)
    {
        if (CurrentScreen is null || !GetSystemTrayRect(TaskbarAreas.TrayArea, out TaskbarRects Rects))
            return new Point(0, 0);

        // Use the full taskbar rectangle.
        NativeMethods.RECT TaskbarRect = Rects.TrayRect;

        double X;
        double Y;

        // If the potion isn't within the taskbar (shouldn't happen), default to bottom.
        if (!(position.X >= TaskbarRect.Left && position.X < TaskbarRect.Right && position.Y >= TaskbarRect.Top && position.Y < TaskbarRect.Bottom))
        {
            AlignedToBottom(position, size, TaskbarRect, out X, out Y);
        }
        else
        {
            // Otherwise, check where the taskbar is, and calculate an aligned position.
            switch (GetTaskBarLocation(TaskbarRect))
            {
                case TaskBarSide.Top:
                    AlignedToTop(position, size, TaskbarRect, out X, out Y);
                    break;

                default:
                case TaskBarSide.Bottom:
                    AlignedToBottom(position, size, TaskbarRect, out X, out Y);
                    break;

                case TaskBarSide.Left:
                    AlignedToLeft(position, size, TaskbarRect, out X, out Y);
                    break;

                case TaskBarSide.Right:
                    AlignedToRight(position, size, TaskbarRect, out X, out Y);
                    break;
            }
        }

        return new Point(X, Y);
    }
    #endregion

    #region Implementation
    private enum TaskBarSide
    {
        Top,
        Bottom,
        Left,
        Right,
    }

    private static TaskBarSide GetTaskBarLocation(NativeMethods.RECT taskbarRect)
    {
        GetQuadrant(taskbarRect, out bool IsLeft, out bool IsRight, out bool IsTop, out bool IsBottom);

        return IsTop && !IsLeft && !IsRight
            ? TaskBarSide.Top
            : IsBottom && !IsLeft && !IsRight
            ? TaskBarSide.Bottom
            : IsLeft && !IsTop && !IsBottom
            ? TaskBarSide.Left
            : IsRight && !IsTop && !IsBottom ? TaskBarSide.Right : TaskBarSide.Bottom;
    }

    private static void GetQuadrant(NativeMethods.RECT taskbarRect, out bool isLeft, out bool isRight, out bool isTop, out bool isBottom)
    {
        Point TaskbarCenter = GetTaskBarRectCenter(taskbarRect);

        int? WorkingAreaWidthQuarter = GetWorkingAreaWidthQuarter();
        int? WorkingAreaHeightQuarter = GetWorkingAreaHeightQuarter();
        isLeft = TaskbarCenter.X < CurrentScreen?.WorkingArea.Left + WorkingAreaWidthQuarter;
        isRight = TaskbarCenter.X >= CurrentScreen?.WorkingArea.Right - WorkingAreaWidthQuarter;
        isTop = TaskbarCenter.Y < CurrentScreen?.WorkingArea.Top + WorkingAreaHeightQuarter;
        isBottom = TaskbarCenter.Y >= CurrentScreen?.WorkingArea.Bottom - WorkingAreaHeightQuarter;
    }

    private static Point GetTaskBarRectCenter(NativeMethods.RECT taskbarRect)
    {
        double RectCenterX = (taskbarRect.Left + taskbarRect.Right) / 2;
        double RectCenterY = (taskbarRect.Top + taskbarRect.Bottom) / 2;
        return new Point(RectCenterX, RectCenterY);
    }

    private static int? GetWorkingAreaWidthQuarter() => (CurrentScreen?.WorkingArea.Right - CurrentScreen?.WorkingArea.Left) / 4;

    private static int? GetWorkingAreaHeightQuarter() => (CurrentScreen?.WorkingArea.Bottom - CurrentScreen?.WorkingArea.Top) / 4;

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

    private static bool GetSystemTrayRect(TaskbarAreas taskbarAreas, out TaskbarRects rects)
    {
        bool Result = false;
        rects = new(
            TrayRect: new NativeMethods.RECT()
            {
                Left = 0,
                Top = 0,
                Right = 0,
                Bottom = 0,
            },
            NotificationRect: new NativeMethods.RECT()
            {
                Left = 0,
                Top = 0,
                Right = 0,
                Bottom = 0,
            },
            IconRect: new NativeMethods.RECT()
            {
                Left = 0,
                Top = 0,
                Right = 0,
                Bottom = 0,
            });

        IntPtr ShellTrayWnd = NativeMethods.FindWindow("Shell_TrayWnd", null);
        if (ShellTrayWnd != IntPtr.Zero)
        {
            if (taskbarAreas.HasFlag(TaskbarAreas.TrayArea))
            {
                NativeMethods.RECT Rect = new() { Left = 0, Top = 0, Right = 0, Bottom = 0 };
                _ = NativeMethods.GetWindowRect(ShellTrayWnd, ref Rect);
                rects = rects with { TrayRect = Rect };

                Result = true;
            }

            if (taskbarAreas.HasFlag(TaskbarAreas.NotificationArea) || taskbarAreas.HasFlag(TaskbarAreas.IconArea))
            {
                Result = false;

                IntPtr TrayNotifyWnd = NativeMethods.FindWindowEx(ShellTrayWnd, IntPtr.Zero, "TrayNotifyWnd", null);
                if (TrayNotifyWnd != IntPtr.Zero)
                {
                    if (taskbarAreas.HasFlag(TaskbarAreas.NotificationArea))
                    {
                        NativeMethods.RECT Rect = new() { Left = 0, Top = 0, Right = 0, Bottom = 0 };
                        _ = NativeMethods.GetWindowRect(TrayNotifyWnd, ref Rect);
                        rects = rects with { NotificationRect = Rect };

                        Result = true;
                    }

                    if (taskbarAreas.HasFlag(TaskbarAreas.IconArea))
                    {
                        Result = false;

                        IntPtr SysPagerWnd = NativeMethods.FindWindowEx(TrayNotifyWnd, IntPtr.Zero, "SysPager", null);
                        if (SysPagerWnd != IntPtr.Zero)
                        {
                            IntPtr ToolbarWnd = NativeMethods.FindWindowEx(SysPagerWnd, IntPtr.Zero, "ToolbarWindow32", null);
                            if (ToolbarWnd != IntPtr.Zero)
                            {
                                NativeMethods.RECT Rect = new() { Left = 0, Top = 0, Right = 0, Bottom = 0 };
                                _ = NativeMethods.GetWindowRect(ToolbarWnd, ref Rect);
                                rects = rects with { NotificationRect = Rect };

                                return true;
                            }
                        }
                    }
                }
            }
        }

        return Result;
    }
    #endregion
}
