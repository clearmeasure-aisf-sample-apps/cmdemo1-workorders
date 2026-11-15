using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UnitTests;

/// <summary>
/// A hand-written <see cref="IJSRuntime"/> or <see cref="IJSObjectReference"/> test double: one
/// <see cref="Answer{TValue}"/> serves both <c>InvokeAsync</c> overloads.
/// </summary>
/// <remarks>
/// BL0016 reports every call a JS interop type makes to its own members as unguarded interop, so a double whose
/// token overload forwards to the other overload does not build. The double answers through this interface instead.
/// </remarks>
internal interface IJsInteropDouble
{
    ValueTask<TValue> Answer<TValue>(string identifier, object?[]? args);
}

/// <summary>Routes both <c>InvokeAsync</c> overloads of a <see cref="IJsInteropDouble"/> to its one answer.</summary>
internal static class JsInteropDouble
{
    public static ValueTask<TValue> Answer<TValue>(IJsInteropDouble jsDouble, string identifier, object?[]? args) =>
        jsDouble.Answer<TValue>(identifier, args);
}