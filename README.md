# TaskbarTools
Create an icon on the Windows taskbar and manage its menu.

[![Build status](https://ci.appveyor.com/api/projects/status/pit8rfvu7s3pxg79?svg=true)](https://ci.appveyor.com/project/dlebansais/taskbartools) [![CodeFactor](https://www.codefactor.io/repository/github/dlebansais/taskbartools/badge)](https://www.codefactor.io/repository/github/dlebansais/taskbartools)  [![NuGet](https://img.shields.io/nuget/v/TaskbarTools.svg)](https://www.nuget.org/packages/TaskbarTools)

## Requirements

This tool requires .NET Framework 4.8, and you must add a reference to System.Drawing in your project. 

## Creating the taskbar icon

To create the taskbar icon, you must provide an actual icon. You can get it from various sources, for instance:

+ A PNG bitmap

````
Bitmap ResourceBitmap = new Bitmap("MyFile.png");
IntPtr Handle = ResourceBitmap.GetHicon();
Icon TemporaryIcon = Icon.FromHandle(Handle);
Icon ResourceIcon = (Icon)TemporaryIcon.Clone();
TemporaryIcon.Dispose();
````

+ A resource icon
 
````
// In MyFile.ico properties, choose "Embedded Resource". 
Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
using (Stream ResourceStream = CurrentAssembly.GetManifestResourceStream("MyNamespace.MyFile.ico"))
{
    Icon ResourceIcon = new Icon(ResourceStream);
	/* ... */
}
````

You can also associate a tooltip and a menu to the icon. For the tooltip, just specify your text, or null, when calling `Create`. To change the tooltip, call `UpdateToolTipText`.

The menu must be prepared before calling `Create`. It must be a `ContextMenu`, and if one of the menu items is either not visible or disabled at start, you need to call `PrepareMenuItem` for that item.

Finally, you need to specify the control that will receive menu commands, usually your main window.

## Events

The tool supports two events:

+ `MenuOpening`. Called before the menu is displayed.
+ `IconClicked`. Called when the user clicks on the taskbar icon.

## Methods

You can update the menu associated to the taskbar icon with these self-explanatory methods:

    ToggleMenuCheck
    IsMenuChecked
    SetMenuCheck
    SetMenuText
    SetMenuIsVisible
    SetMenuIsEnabled
    SetMenuIcon

The last method, `SetMenuIcon`, can take a `Bitmap` or an `Icon`.

You can also change the taskbar icon itself with the `UpdateIcon` method.

## Removing the taskbar icon

The icon is removed from the taskbar when you dispose of the `TaskbarIcon` object with a `using` statement.
 
## Nullable support

To avoid declaring a nullable type, you can use the static value `TaskbarIcon.Empty`.

## Taskbar Location

This assembly also provides additional info about the location of the taskbar and its content.

### TaskbarLocation.ScreenBounds

This property gets the bounds of the current screen used to display the taskbar.

### TaskbarLocation.GetRelativePosition

	Point GetRelativePosition(FrameworkElement element);

Return the position a FrameworkElement should take to be on the edge of the task bar (in screen coordinates). This is useful to display popups.

