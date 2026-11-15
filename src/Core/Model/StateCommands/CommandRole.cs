namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>
/// The workflow role of a state command: advances the work order, a secondary action, or cancels it.
/// </summary>
public enum CommandRole
{
    /// <summary>Advances the work order along the happy path (Assign, Begin, Complete).</summary>
    Forward,

    /// <summary>A non-critical action such as Save, Shelve, Wait on vendor, or Resume.</summary>
    Secondary,

    /// <summary>Cancels the work order.</summary>
    Destructive
}
