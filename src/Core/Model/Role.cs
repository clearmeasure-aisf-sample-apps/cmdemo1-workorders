// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

namespace ClearMeasure.Bootcamp.Core.Model;

public class Role(string name, bool canCreate, bool canFulfill) : EntityBase<Role>
{
    public Role() : this(null!, false, false)
    {
    }

    public string Name { get; set; } = name;

    public bool CanCreateWorkOrder { get; set; } = canCreate;

    public bool CanFulfillWorkOrder { get; set; } = canFulfill;
    public override Guid Id { get; set; }

    public override string ToString()
    {
        // string.Concat treats a null argument as empty, matching AC without a nullable ?? that Qodana rejects on non-nullable Name
        return string.Concat(Name);
    }
}