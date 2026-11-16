namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// An outside company or tradesperson the church hires for maintenance work.
/// </summary>
public class Vendor : EntityBase<Vendor>
{
    /// <summary>
    /// Maximum length of <see cref="Name"/>.
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Maximum length of <see cref="Trade"/>.
    /// </summary>
    public const int TradeMaxLength = 100;

    /// <summary>
    /// Maximum length of <see cref="ContactName"/>.
    /// </summary>
    public const int ContactNameMaxLength = 100;

    /// <summary>
    /// Maximum length of <see cref="EmailAddress"/>.
    /// </summary>
    public const int EmailAddressMaxLength = 255;

    /// <summary>
    /// Maximum length of <see cref="PhoneNumber"/>.
    /// </summary>
    public const int PhoneNumberMaxLength = 50;

    /// <summary>
    /// Maximum length of <see cref="Notes"/>.
    /// </summary>
    public const int NotesMaxLength = 1000;

    /// <summary>
    /// Creates an empty, active vendor. Used by EF Core and serialization.
    /// </summary>
    public Vendor()
    {
        Name = null!;
    }

    /// <summary>
    /// Unique identifier of the vendor.
    /// </summary>
    public override Guid Id { get; set; }

    /// <summary>
    /// Name of the vendor, unique ignoring case.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Optional trade, such as HVAC or plumbing.
    /// </summary>
    public string? Trade { get; init; }

    /// <summary>
    /// Optional name of the person to contact at the vendor.
    /// </summary>
    public string? ContactName { get; init; }

    /// <summary>
    /// Optional email address of the vendor.
    /// </summary>
    public string? EmailAddress { get; init; }

    /// <summary>
    /// Optional phone number of the vendor.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Optional notes about the vendor.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Whether the church still uses this vendor. New vendors are active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}
