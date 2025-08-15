namespace TaskbarTools;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

/// <summary>
/// This class provides an API to display notifications to the user.
/// </summary>
public static class TaskbarBalloon
{
    #region Init
    static TaskbarBalloon()
    {
        DefaultDelay = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Gets the default delay before a balloon closes.
    /// </summary>
    public static TimeSpan DefaultDelay { get; }
    #endregion

    #region Client Interface
    /// <summary>
    /// Displays a notification in a taskbar balloon.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <param name="delay">The delay, in milliseconds.</param>
    public static void Show(string text, TimeSpan delay) => Show(text, delay, TimeSpan.Zero);

    /// <summary>
    /// Displays a notification in a taskbar balloon.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <param name="delay">The delay, in milliseconds.</param>
    /// <param name="delayWait">The delay waiting synchronously to ensure the balloon is entirely visible upon return.</param>
    public static void Show(string text, TimeSpan delay, TimeSpan delayWait)
    {
        using NotifyIcon Notification = new() { Visible = true, Icon = SystemIcons.Shield, Text = ShortString(text), BalloonTipText = ShortString(text) };

        BallonPrivateData? Data = null;
        try
        {
            Data = new(Notification);
            InitializeNotification(delay, Notification, Data);
            DisplayedBalloonList.Add(Data);

            Data = null;
        }
        finally
        {
            Data?.Dispose();
        }

        Thread.Sleep(delayWait);
    }

    /// <summary>
    /// Displays a notification in a taskbar balloon.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <param name="delay">The delay, in milliseconds.</param>
    /// <param name="clickHandler">Handler for the click event.</param>
    /// <param name="clickData">Handler data for the click event.</param>
    /// <exception cref="NullReferenceException"><paramref name="text"/> is null.</exception>
    public static void Show(string text, TimeSpan delay, Action<object> clickHandler, object clickData)
    {
        NotifyIcon NotifyIcon = new() { Visible = true, Icon = SystemIcons.Shield, Text = ShortString(text), BalloonTipText = ShortString(text) };
        BallonPrivateData Data = new(NotifyIcon, clickHandler, clickData, leaveOpen: false);
        InitializeNotification(delay, Data.Notification, Data);
        DisplayedBalloonList.Add(Data);
    }
    #endregion

    #region Implementation
    private static string ShortString(string? text)
    {
        if (text is not null && text.Length >= 16)
#if NETFRAMEWORK
            return text[..8] + "..." + text.Substring(text.Length - 8, 8);
#else
            return string.Concat(text.AsSpan(8), "---", text.AsSpan(text.Length - 8, 8));
#endif
        else
            return text is not null ? text : string.Empty;
    }

    private static void InitializeNotification(TimeSpan delay, NotifyIcon notification, BallonPrivateData data)
    {
        try
        {
            notification.Tag = data;
            notification.BalloonTipClosed += new EventHandler(OnClosed);
            notification.BalloonTipClicked += new EventHandler(OnClicked);
            notification.Click += new EventHandler(OnClicked);
            notification.MouseClick += new MouseEventHandler(OnMouseClicked);
            notification.ShowBalloonTip((int)delay.TotalMilliseconds);
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            Debug.WriteLine($"Error in InitializeNotification: {exception.Message}");
        }
    }

    private static readonly List<BallonPrivateData> DisplayedBalloonList = [];

    private static void OnClosed(object? sender, EventArgs e) => BallonCloseHandler(sender);

    private static void OnClicked(object? sender, EventArgs e)
    {
        BallonClickHandler(sender);
        BallonCloseHandler(sender);
    }

    private static void OnMouseClicked(object? sender, MouseEventArgs e)
    {
        BallonClickHandler(sender);
        BallonCloseHandler(sender);
    }

    private static void BallonClickHandler(object? sender)
    {
        if (sender is NotifyIcon Notification && Notification.Tag is BallonPrivateData Data)
        {
            if (Data.GetClickHandler(out Action<object> ClickHandler, out object ClickData))
                ClickHandler.Invoke(ClickData);
        }
    }

    private static void BallonCloseHandler(object? sender)
    {
        if (sender is NotifyIcon Notification)
        {
            Notification.Visible = false;

            if (Notification.Tag is BallonPrivateData Data)
            {
                Notification.Tag = null;
                _ = DisplayedBalloonList.Remove(Data);
                Data.Closed();
#pragma warning disable IDISP007 // Don't dispose injected
                Data.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected
            }

#pragma warning disable IDISP007 // Don't dispose injected
            Notification.Dispose();
#pragma warning restore IDISP007 // Don't dispose injected
        }
    }

    private sealed class BallonPrivateData : IDisposable
    {
        public BallonPrivateData(NotifyIcon notification, bool leaveOpen = false)
        {
            Notification = notification;
            ClickData = this;
            LeaveOpen = leaveOpen;
        }

        public BallonPrivateData(NotifyIcon notification, Action<object> clickHandler, object clickData, bool leaveOpen = false)
            : this(notification)
        {
            ClickHandler = clickHandler;
            ClickData = clickData;
            LeaveOpen = leaveOpen;
        }

        public NotifyIcon Notification { get; }
        private readonly bool LeaveOpen;
        public Action<object>? ClickHandler { get; private set; }
        public object ClickData { get; init; }
        public bool IsClosed { get; private set; }
        private bool disposedValue;

        public void Closed() => IsClosed = true;

        public bool GetClickHandler(out Action<object> clickHandler, out object clickData)
        {
            clickData = ClickData;

            if (ClickHandler is not null)
            {
                clickHandler = ClickHandler;
                ClickHandler = null;
                return true;
            }
            else
            {
                clickHandler = new Action<object>(data => { });
                return false;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!LeaveOpen)
                        Notification?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~BallonPrivateData()
        {
            Dispose(disposing: false);
        }
    }
    #endregion
}
