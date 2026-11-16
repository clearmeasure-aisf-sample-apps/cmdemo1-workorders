using System.Diagnostics.CodeAnalysis;

namespace ClearMeasure.Bootcamp.Core;

/// <summary>
/// Marks a request, command, query, or event that travels through <see cref="IBus"/>.
/// </summary>
/// <remarks>
/// The bus writes each message's scalar properties to its tracing activity. The
/// <see cref="DynamicallyAccessedMembersAttribute"/> on this interface tells the trimmer and the
/// Native AOT compiler to keep the public properties of every type that implements it, so that
/// reflection stays safe when the app is trimmed.
/// </remarks>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public interface IBusMessage;