namespace TaskbarTools;

using System;

/// <summary>
/// Represents an exception thrown when the API could not create the icon.
/// </summary>
[Serializable]
public class IconCreationFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IconCreationFailedException"/> class.
    /// </summary>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    internal IconCreationFailedException(Exception innerException)
        : base(innerException.Message, innerException)
    {
    }
}
