using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

/// <summary>
/// Stores a <see cref="WorkOrderStatus"/> as its three-letter code.
/// </summary>
/// <remarks>
/// Returns a plain <see cref="ValueConverter{TModel,TProvider}"/>, not a subclass: EF's compiled model (Native AOT)
/// re-creates converters as <c>ValueConverter&lt;,&gt;</c>, while precompiled queries cast to the converter type seen at
/// design time, so a subclass fails at run time with InvalidCastException.
/// </remarks>
public static class WorkOrderStatusConverter
{
    public static ValueConverter<WorkOrderStatus, string> Create() =>
        new(v => v.Code, v => WorkOrderStatus.FromCode(v));
}