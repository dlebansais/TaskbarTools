namespace TaskbarTools;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Contracts;

/// <summary>
/// Represents a custom icon added to the taskbar.
/// </summary>
public class TaskbarIcon : IDisposable
{
    #region Init
    /// <summary>
    /// Gets the neutral, empty icon instance for code with nullable enabled.
    /// </summary>
    public static TaskbarIcon Empty { get; } = new TaskbarIcon();

    private TaskbarIcon()
    {
        NotifyIconField = new NotifyIcon();
        Target = Keyboard.FocusedElement;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskbarIcon"/> class.
    /// </summary>
    /// <param name="notifyIcon">The system icon instance.</param>
    /// <param name="target">The target input element for menu interaction. Can be null.</param>
    protected TaskbarIcon(NotifyIcon notifyIcon, IInputElement? target)
    {
        NotifyIconField = notifyIcon;
        Target = target;
    }

    /// <summary>
    /// Gets the list of icons added to the taskbar with this API.
    /// </summary>
    protected static IList<TaskbarIcon> ActiveIconList { get; } = new List<TaskbarIcon>();

    private readonly NotifyIcon NotifyIconField;
    private readonly IInputElement? Target;
    #endregion

    #region Client Interface
    /// <summary>
    /// Creates and displays a taskbar icon.
    /// </summary>
    /// <param name="icon">The icon displayed. The caller is responsible for releasing it when the created instance of <see cref="TaskbarIcon"/> is released.</param>
    /// <param name="toolTipText">The text shown when the mouse is over the icon, can be null.</param>
    /// <param name="menu">The menu that pops up when the user left click the icon, can be null.</param>
    /// <param name="target">The object that receives command notifications, can be null.</param>
    /// <returns>The created taskbar icon object.</returns>
    public static TaskbarIcon Create(Icon icon, string? toolTipText, System.Windows.Controls.ContextMenu? menu, IInputElement? target)
    {
        Contract.RequireNotNull(icon, out Icon Icon);

        try
        {
            NotifyIcon NotifyIcon = new NotifyIcon { Icon = Icon, Text = string.Empty };
            NotifyIcon.Click += OnClick;

            TaskbarIcon NewTaskbarIcon = new TaskbarIcon(NotifyIcon, target);
            NotifyIcon.ContextMenuStrip = NewTaskbarIcon.MenuToMenuStrip(menu);

            ActiveIconList.Add(NewTaskbarIcon);
            NewTaskbarIcon.UpdateToolTipText(toolTipText);
            NotifyIcon.Visible = true;

            return NewTaskbarIcon;
        }
        catch (Exception e)
        {
            throw new IconCreationFailedException(e);
        }
    }

    /// <summary>
    /// Toggles the check mark of a menu item.
    /// </summary>
    /// <param name="command">The command associated to the menu item.</param>
    /// <param name="isChecked">The new value of the check mark.</param>
    public static void ToggleMenuCheck(ICommand command, out bool isChecked)
    {
        Contract.RequireNotNull(command, out ICommand Command);

        ToolStripMenuItem MenuItem = GetMenuItemFromCommand(Command);
        isChecked = !MenuItem.Checked;
        MenuItem.Checked = isChecked;
    }

    /// <summary>
    /// Returns the current check mark of a menu item.
    /// </summary>
    /// <param name="command">The command associated to the menu item.</param>
    /// <returns>True if the menu item has a check mark, false otherwise.</returns>
    public static bool IsMenuChecked(ICommand command)
    {
        Contract.RequireNotNull(command, out ICommand Command);

        ToolStripMenuItem MenuItem = GetMenuItemFromCommand(Command);
        return MenuItem.Checked;
    }

    /// <summary>
    /// Sets the check mark of a menu item. This can be called within a handler of the <see cref="MenuOpening"/> event, the change is applied as the menu pops up.
    /// </summary>
    /// <param name="command">The command associated to the menu item.</param>
    /// <param name="isChecked">True if the menu item must have a check mark, false otherwise.</param>
    public static void SetMenuCheck(ICommand command, bool isChecked)
    {
        Contract.RequireNotNull(command, out ICommand Command);

        ToolStripMenuItem MenuItem = GetMenuItemFromCommand(Command);
        MenuItem.Checked = isChecked;
    }

    /// <summary>
    /// Sets the text of menu item. This can be called within a handler of the <see cref="MenuOpening"/> event, the change is applied as the menu pops up.
    /// </summary>
    /// <param name="command">The command associated to the menu item.</param>
    /// <param name="text">The new menu item text.</param>
    public static void SetMenuText(ICommand command, string text)
    {
        Contract.RequireNotNull(command, out ICommand Command);
        Contract.RequireNotNull(text, out string Text);

        ToolStripMenuItem MenuItem = GetMenuItemFromCommand(Command);
        MenuItem.Text = Text;
    }

    /// <summary>
    /// Shows or hides the menu item. This can be called within a handler of the <see cref="MenuOpening"/> event, the change is applied as the menu pops up.
    /// </summary>
    /// <param name="command">The command associated to the menu item.</param>
    /// <param name="isVisible">True to show the menu item.</param>
    public static void SetMenuIsVisible(ICommand command, bool isVisible)
    {
        Contract.RequireNotNull(command, out ICommand Command);

        ToolStripMenuItem MenuItem = GetMenuItemFromCommand(Command);
        MenuItem.Visible = isVisible;
    }

    /// <summary>
    /// Enables or disables the menu item. This can be called within a handler of the <see cref="MenuOpening"/> event, the change is applied as the menu pops up.
    /// </summary>
    /// <param name="command">The command associated to the menu item.</param>
    /// <param name="isEnabled">True if enabled.</param>
    public static void SetMenuIsEnabled(ICommand command, bool isEnabled)
    {
        Contract.RequireNotNull(command, out ICommand Command);

        ToolStripMenuItem MenuItem = GetMenuItemFromCommand(Command);
        MenuItem.Enabled = isEnabled;
    }

    /// <summary>
    /// Sets the menu item icon. This can be called within a handler of the <see cref="MenuOpening"/> event, the change is applied as the menu pops up.
    /// </summary>
    /// <param name="command">The command associated to the menu item.</param>
    /// <param name="icon">The icon to set, null for no icon.</param>
    public static void SetMenuIcon(ICommand command, Icon? icon)
    {
        Contract.RequireNotNull(command, out ICommand Command);

        ToolStripMenuItem MenuItem = GetMenuItemFromCommand(Command);

        if (icon is not null)
            MenuItem.Image = icon.ToBitmap();
        else
            MenuItem.Image = null;
    }

    /// <summary>
    /// Sets the menu item icon. This can be called within a handler of the <see cref="MenuOpening"/> event, the change is applied as the menu pops up.
    /// </summary>
    /// <param name="command">The command associated to the menu item.</param>
    /// <param name="bitmap">The icon to set, as a bitmap, null for no icon.</param>
    public static void SetMenuIcon(ICommand command, Bitmap? bitmap)
    {
        Contract.RequireNotNull(command, out ICommand Command);

        ToolStripMenuItem MenuItem = GetMenuItemFromCommand(Command);
        MenuItem.Image = bitmap;
    }

    /// <summary>
    /// Changes the taskbar icon.
    /// </summary>
    /// <param name="icon">The new icon displayed. The caller is responsible for releasing it as well as the old icon.</param>
    public void UpdateIcon(Icon icon)
    {
        Icon Icon = Contract.RequireNotNull(icon);

        AssertNotEmpty();

        SetNotifyIcon(NotifyIconField, Icon);
    }

    /// <summary>
    /// Sets the tool tip text displayed when the mouse is over the taskbar icon.
    /// </summary>
    /// <param name="toolTipText">The new tool tip text.</param>
    public void UpdateToolTipText(string? toolTipText)
    {
        AssertNotEmpty();

        // Various versions of windows have length limitations (documented as usual).
        // We remove extra lines until it works...
        while (true)
        {
            try
            {
                SetNotifyIconText(NotifyIconField, toolTipText);
                return;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                if (toolTipText is not null && toolTipText.Length > 0)
                {
                    string[] Split = toolTipText.Split('\r');

                    toolTipText = string.Empty;
                    for (int i = 0; i + 1 < Split.Length; i++)
                    {
                        if (i > 0)
                            toolTipText += "\r";

                        toolTipText += Split[i];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Prepares a menu item before is is added to a menu, before calling <see cref="Create"/>.
    /// Calling this method is required only if either <paramref name="isVisible"/> or <paramref name="isEnabled"/> is false.
    /// </summary>
    /// <param name="item">The modified menu item.</param>
    /// <param name="isVisible">True if the menu should be visible.</param>
    /// <param name="isEnabled">True if the menu should be enabled.</param>
    public static void PrepareMenuItem(System.Windows.Controls.MenuItem item, bool isVisible, bool isEnabled)
    {
        Contract.RequireNotNull(item, out System.Windows.Controls.MenuItem Item);

        Item.Visibility = isVisible ? (isEnabled ? Visibility.Visible : Visibility.Hidden) : Visibility.Collapsed;
    }

    private void AssertNotEmpty()
    {
        if (this == Empty)
            throw new ArgumentException("Method call on TaskbarIcon.Empty not allowed");
    }
    #endregion

    #region Implementation
    private static void SetNotifyIconText(NotifyIcon ni, string? text)
    {
        SetNotifyIconValue(ni, ToFrameworkSpecificFieldName("text"), text);
    }

    private static void SetNotifyIcon(NotifyIcon ni, Icon icon)
    {
        SetNotifyIconValue(ni, ToFrameworkSpecificFieldName("icon"), icon);
    }

    private static void SetNotifyIconValue(NotifyIcon ni, string valueName, object? value)
    {
        Type t = typeof(NotifyIcon);
        const BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;

        Contract.RequireNotNull(t.GetField(valueName, hidden), out FieldInfo FieldInfoName);
        FieldInfoName.SetValue(ni, value);

        Contract.RequireNotNull(t.GetField(ToFrameworkSpecificFieldName("added"), hidden), out FieldInfo FieldInfoIsAdded);
        bool? IsAddedValue = (bool?)FieldInfoIsAdded.GetValue(ni);

        if (IsAddedValue.HasValue && IsAddedValue.Value == true)
        {
            Contract.RequireNotNull(t.GetMethod("UpdateIcon", hidden), out MethodInfo MethodInfoUpdateIcon);
            MethodInfoUpdateIcon.Invoke(ni, new object[] { true });
        }
    }

    private static ToolStripMenuItem GetMenuItemFromCommand(ICommand command)
    {
        foreach (KeyValuePair<ToolStripMenuItem, ICommand> Entry in CommandTable)
            if (Entry.Value == command)
                return Entry.Key;

        throw new InvalidCommandException(command);
    }

    private static string ToFrameworkSpecificFieldName(string name)
    {
#if NETFRAMEWORK
        return name;
#else
        return $"_{name}";
#endif
    }
    #endregion

    #region Events
    /// <summary>
    /// Event raised before the menu pops up.
    /// </summary>
    public event EventHandler? MenuOpening;

    /// <summary>
    /// Event raised when the icon is clicked.
    /// </summary>
    public event EventHandler? IconClicked;

    private static void OnClick(object? sender, EventArgs e)
    {
        if (e is System.Windows.Forms.MouseEventArgs AsMouseEventArgs)
        {
            foreach (TaskbarIcon Item in ActiveIconList)
                if (Item.NotifyIconField == sender)
                {
                    Item.OnClick(AsMouseEventArgs.Button);
                    break;
                }
        }
    }

    /// <summary>
    /// Called when a mouse button has been clicked on the taskbar icon.
    /// </summary>
    /// <param name="button">The mouse button.</param>
    protected void OnClick(MouseButtons button)
    {
        switch (button)
        {
            case MouseButtons.Left:
                IconClicked?.Invoke(this, EventArgs.Empty);
                break;

            case MouseButtons.Right:
                MenuOpening?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private static void OnMenuClicked(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem MenuItem)
            OnMenuClicked(MenuItem);
    }
    #endregion

    #region Menu
    private ContextMenuStrip? MenuToMenuStrip(System.Windows.Controls.ContextMenu? menu)
    {
        ContextMenuStrip? Result = null;

        if (menu is not null)
        {
            Result = new ContextMenuStrip();
            ConvertToolStripMenuItems(menu.Items, Result.Items);
        }

        return Result;
    }

    private void ConvertToolStripMenuItems(System.Windows.Controls.ItemCollection sourceItems, ToolStripItemCollection destinationItems)
    {
        foreach (System.Windows.Controls.Control? Item in sourceItems)
            if (Item is System.Windows.Controls.MenuItem AsMenuItem)
                if (AsMenuItem.Items.Count > 0)
                    AddSubmenuItem(destinationItems, AsMenuItem);
                else
                    AddMenuItem(destinationItems, AsMenuItem);
            else if (Item is System.Windows.Controls.Separator)
                AddSeparator(destinationItems);
    }

    private void AddSubmenuItem(ToolStripItemCollection destinationItems, System.Windows.Controls.MenuItem menuItem)
    {
        string MenuHeader = (string)menuItem.Header;
        ToolStripMenuItem NewMenuItem = new ToolStripMenuItem(MenuHeader);

        ConvertToolStripMenuItems(menuItem.Items, NewMenuItem.DropDownItems);

        destinationItems.Add(NewMenuItem);
    }

    private void AddMenuItem(ToolStripItemCollection destinationItems, System.Windows.Controls.MenuItem menuItem)
    {
        string MenuHeader = (string)menuItem.Header;

        ToolStripMenuItem NewMenuItem;

        if (menuItem.Icon is Bitmap MenuBitmap)
            NewMenuItem = new ToolStripMenuItem(MenuHeader, MenuBitmap);
        else if (menuItem.Icon is Icon MenuIcon)
            NewMenuItem = new ToolStripMenuItem(MenuHeader, MenuIcon.ToBitmap());
        else
            NewMenuItem = new ToolStripMenuItem(MenuHeader);

        NewMenuItem.Click += OnMenuClicked;

        // See PrepareMenuItem for using the visibility to carry Visible/Enabled flags
        NewMenuItem.Visible = menuItem.Visibility != Visibility.Collapsed;
        NewMenuItem.Enabled = menuItem.Visibility == Visibility.Visible;
        NewMenuItem.Checked = menuItem.IsChecked;

        destinationItems.Add(NewMenuItem);
        MenuTable.Add(NewMenuItem, this);
        CommandTable.Add(NewMenuItem, menuItem.Command);
    }

    private static void AddSeparator(ToolStripItemCollection destinationItems)
    {
        ToolStripSeparator NewSeparator = new ToolStripSeparator();
        destinationItems.Add(NewSeparator);
    }

    private static void OnMenuClicked(ToolStripMenuItem menuItem)
    {
        if (MenuTable.TryGetValue(menuItem, out TaskbarIcon? TaskbarIcon) && CommandTable.TryGetValue(menuItem, out ICommand? Command))
        {
            if (Command is RoutedCommand AppCommand && TaskbarIcon.Target is not null)
                AppCommand.Execute(TaskbarIcon, TaskbarIcon.Target);
        }
    }

    private static readonly Dictionary<ToolStripMenuItem, TaskbarIcon> MenuTable = new Dictionary<ToolStripMenuItem, TaskbarIcon>();
    private static readonly Dictionary<ToolStripMenuItem, ICommand> CommandTable = new Dictionary<ToolStripMenuItem, ICommand>();
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
    /// Finalizes an instance of the <see cref="TaskbarIcon"/> class.
    /// </summary>
    ~TaskbarIcon()
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
        using NotifyIcon ToRemove = NotifyIconField;

        ToRemove.Visible = false;
        ToRemove.Click -= OnClick;

        foreach (TaskbarIcon Item in ActiveIconList)
            if (Item.NotifyIconField == NotifyIconField)
            {
                ActiveIconList.Remove(Item);
                break;
            }
    }
    #endregion
}
