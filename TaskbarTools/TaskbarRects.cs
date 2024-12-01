namespace TaskbarTools;

/// <summary>
/// Represents rectangles of the taskbar areas.
/// </summary>
/// <param name="TrayRect">The tray area.</param>
/// <param name="NotificationRect">The notification area.</param>
/// <param name="IconRect">The icon are.</param>
internal record TaskbarRects(NativeMethods.RECT TrayRect, NativeMethods.RECT NotificationRect, NativeMethods.RECT IconRect);
