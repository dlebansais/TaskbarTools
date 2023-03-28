namespace TaskbarTools;

using System;
using System.Windows.Input;

#pragma warning disable CA1032 // Implement standard exception constructors
#pragma warning disable CA2237 // Mark ISerializable types with SerializableAttribute
/// <summary>
/// Represents an exception thrown when an invalid command is provided as a parameter to vaious public methods of the API.
/// For instance: <see cref="TaskbarIcon.ToggleMenuCheck"/>, <see cref="TaskbarIcon.IsMenuChecked"/> and so on.
/// </summary>
public class InvalidCommandException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCommandException"/> class.
    /// </summary>
    /// <param name="command">The invalid command.</param>
    internal InvalidCommandException(ICommand command)
    {
        Command = command;
    }

    /// <summary>
    /// Gets the invalid command.
    /// </summary>
    public ICommand Command { get; init; }
}
#pragma warning restore CA2237 // Mark ISerializable types with SerializableAttribute
#pragma warning restore CA1032 // Implement standard exception constructors
