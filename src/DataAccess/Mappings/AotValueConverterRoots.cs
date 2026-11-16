using System.Runtime.CompilerServices;
using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

/// <summary>
/// Native AOT only (compiled when <c>McpServerAot=true</c>). When a precompiled query has to build its SQL at run time
/// (for example, a query with nullable parameters), EF looks up the store mapping of each enum column, and its
/// converter selector (ValueConverterSelector) reads <c>DefaultInfo</c> from each candidate converter type by
/// reflection. Native AOT cannot create those generic types at run time, so this class creates them ahead of time.
/// </summary>
internal static class AotValueConverterRoots
{
    [ModuleInitializer]
    internal static void Root()
    {
        _ = EnumToNumberConverter<WorkOrderPriority, int>.DefaultInfo;
        _ = EnumToNumberConverter<WorkOrderPriority, long>.DefaultInfo;
        _ = EnumToNumberConverter<WorkOrderPriority, short>.DefaultInfo;
        _ = EnumToNumberConverter<WorkOrderPriority, byte>.DefaultInfo;
        _ = EnumToNumberConverter<WorkOrderPriority, uint>.DefaultInfo;
        _ = EnumToNumberConverter<WorkOrderPriority, ulong>.DefaultInfo;
        _ = EnumToNumberConverter<WorkOrderPriority, ushort>.DefaultInfo;
        _ = EnumToNumberConverter<WorkOrderPriority, sbyte>.DefaultInfo;
        _ = EnumToNumberConverter<WorkOrderPriority, decimal>.DefaultInfo;
        _ = EnumToNumberConverter<WorkOrderPriority, double>.DefaultInfo;
        _ = EnumToNumberConverter<WorkOrderPriority, float>.DefaultInfo;
        _ = EnumToStringConverter<WorkOrderPriority>.DefaultInfo;
        _ = NumberToBytesConverter<int>.DefaultInfo; // enum -> byte[] goes through the underlying type (int)
    }
}