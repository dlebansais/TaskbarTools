# TaskbarTools
Create an icon on the Windows taskbar and manage its menu.

[![Build status](https://ci.appveyor.com/api/projects/status/pit8rfvu7s3pxg79?svg=true)](https://ci.appveyor.com/project/dlebansais/taskbartools)

## Creating the taskbar icon

To create the taskbar icon, you must provide an actual icon. You can get it from various sources, for instance:

+ A PNG bitmap

    Bitmap ResourceBitmap = new Bitmap("MyFile.png");
    IntPtr Handle = ResourceBitmap.GetHicon();
    Icon TemporaryIcon = Icon.FromHandle(Handle);
    Icon ResourceIcon = (Icon)TemporaryIcon.Clone();
    TemporaryIcon.Dispose();

+ A resource icon
 
	// In MyFile.ico properties, choose "Embedded Resource". 
 	Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
	using (Stream ResourceStream = CurrentAssembly.GetManifestResourceStream("MyProject.MyFile.ico"))
	{
        Icon ResourceIcon = new Icon(ResourceStream);

You can also associate a tooltip and a menu to the icon. For the tooltip, just specify your text, or null, when calling `Create`. To change the tooltip, call `UpdateToolTipText`.

The menu must be prepared before calling `Create`. It must be a `ContextMenu`, and if one of the menu items is either not visible or disabled at start, you need to call `PrepareMenuItem` first for that item.

Finally, you need to specify the control that will receive menu commands, usually your main window.

## Events

The tool supports two events:

+ MenuOpening. Called before the menu is displayed.
+ IconClicked. Called when the user clicks on the taskbar icon.

## Methods

You can update the menu associated to the taskbar icon with these self-explanatory methods:

    ToggleMenuCheck
    IsMenuChecked
    SetMenuCheck
    SetMenuText
    SetMenuIsVisible
    SetMenuIsEnabled
    SetMenuIcon

The last method, `SetMenuIcon`, can take a `Bitmap` or `Icon`.

You can also change the taskbar icon itself with the `UpdateIcon` method.

## Nullable support

To avoid declaring a nullable type, you can use the static value `TaskbarIcon.Empty`.

