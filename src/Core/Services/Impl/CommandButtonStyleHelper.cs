namespace ClearMeasure.Bootcamp.Core.Services.Impl;

/// <summary>
/// Determines the CSS button style (class) for a state command button based on the command's role.
/// </summary>
public static class CommandButtonStyleHelper
{
    /// <summary>
    /// Gets the button style for a state command. Returns one primary button (the command that moves
    /// the work order forward), secondary buttons for save and other non-critical actions, and a
    /// danger button for cancel commands.
    /// </summary>
    /// <param name="commandName">The command's TransitionVerbPresentTense (e.g., "Save", "Assign", "Cancel")</param>
    /// <param name="validCommandNames">Names of all valid commands for this work order state</param>
    /// <returns>The CSS class suffix (e.g., "btn-primary", "btn-outline-secondary", "btn-outline-danger")</returns>
    public static string GetButtonStyle(string commandName, IEnumerable<string> validCommandNames)
    {
        if (commandName == "Cancel")
        {
            return "btn-outline-danger";
        }

        if (commandName == "Save")
        {
            return "btn-outline-secondary";
        }

        // Determine if this command moves the work order forward (primary action)
        if (IsPrimaryForwardCommand(commandName, validCommandNames))
        {
            return "btn-primary";
        }

        return "btn-outline-secondary";
    }

    /// <summary>
    /// Determines if a command is a primary forward-moving command (the one that advances the workflow).
    /// In any given state, only one command moves the work order forward; all others are secondary.
    /// </summary>
    private static bool IsPrimaryForwardCommand(string commandName, IEnumerable<string> validCommandNames)
    {
        var validNames = validCommandNames.ToList();

        // Map of state transitions that are the primary forward action for each state
        var primaryCommands = new[]
        {
            "Assign",    // Draft -> Assigned (primary forward action in Draft state)
            "Begin",     // Assigned -> InProgress (primary forward action in Assigned state)
            "Complete"   // InProgress -> Complete (primary forward action in InProgress state)
        };

        return primaryCommands.Contains(commandName) && validNames.Contains(commandName);
    }
}
