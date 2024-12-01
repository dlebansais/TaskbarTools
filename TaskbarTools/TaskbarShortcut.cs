namespace TaskbarTools;

using System;
using System.IO;

/// <summary>
/// Helper that manipulates shortcuts of applications pinned to the taskbar.
/// </summary>
public static class TaskbarShortcut
{
    /// <summary>
    /// Gets the content of a shortcut on the taskbar.
    /// </summary>
    /// <param name="shortcutFileName">The shotcut file name (.lnk).</param>
    /// <param name="iconFile">The path to the icon file in the shortcut upon return.</param>
    /// <returns>True if the shortcut contains a path to an icon file.</returns>
    public static bool GetTaskbarShortcut(string shortcutFileName, out string iconFile)
    {
        return GetTaskbarShortcutInternal(shortcutFileName, out iconFile);
    }

    /// <summary>
    /// Sets the content of a shortcut on the taskbar.
    /// </summary>
    /// <param name="shortcutFileName">The shotcut file name (.lnk).</param>
    /// <param name="iconFile">The path to the icon file (.ico) to use in the shortcut.</param>
    /// <returns>True if the shortcut was changed to use <paramref name="iconFile"/>.</returns>
    public static bool SetTaskbarShortcut(string shortcutFileName, string iconFile)
    {
        return !File.Exists(iconFile)
            ? throw new ArgumentException($"{nameof(iconFile)} must be the path to an existing file")
            : SetTaskbarShortcutInternal(shortcutFileName, iconFile);
    }

    /// <summary>
    /// Changes the content of a shortcut on the taskbar if not already set to something.
    /// </summary>
    /// <param name="shortcutFileName">The shotcut file name (.lnk).</param>
    /// <param name="iconFile">The path to the icon file (.ico) to use in the shortcut.</param>
    /// <param name="isChanged">True upon return if the shortcut was changed.</param>
    /// <returns>True if the shortcut contains an icon.</returns>
    public static bool UpdateTaskbarShortcut(string shortcutFileName, string iconFile, out bool isChanged)
    {
        if (!File.Exists(iconFile))
            throw new ArgumentException($"{nameof(iconFile)} must be the path to an existing file");

        isChanged = false;

        if (!GetTaskbarShortcutInternal(shortcutFileName, out string ExistingIconFile))
            return false;

        if (ExistingIconFile.Length > 0)
            return true;

        if (!SetTaskbarShortcutInternal(shortcutFileName, iconFile))
            return false;

        isChanged = true;
        return true;
    }

    private static bool GetTaskbarShortcutInternal(string shortcutFileName, out string iconFile)
    {
        iconFile = string.Empty;

        string ShortcutPath = Path.Combine(TaskbarShortcutPath, shortcutFileName);

        if (!File.Exists(ShortcutPath))
            return false;

        Shell32.ShellLinkObject Link = GetShellLink(shortcutFileName);

        _ = Link.GetIconLocation(out iconFile);
        return true;
    }

    private static bool SetTaskbarShortcutInternal(string shortcutFileName, string iconFile)
    {
        string ShortcutPath = Path.Combine(TaskbarShortcutPath, shortcutFileName);

        if (!File.Exists(ShortcutPath))
            return false;

        Shell32.ShellLinkObject Link = GetShellLink(shortcutFileName);

        Link.SetIconLocation(iconFile, 0);
        Link.Save();

        return true;
    }

    private static string TaskbarShortcutPath
    {
        get
        {
            string RoamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(RoamingPath, @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
        }
    }

    private static Shell32.ShellLinkObject GetShellLink(string shortcutFileName)
    {
        Shell32.Shell Shell = new();
        Shell32.Folder Folder = Shell.NameSpace(TaskbarShortcutPath);
        Shell32.FolderItem Item = Folder.ParseName(shortcutFileName);
        Shell32.ShellLinkObject Link = (Shell32.ShellLinkObject)Item.GetLink;

        return Link;
    }
}
