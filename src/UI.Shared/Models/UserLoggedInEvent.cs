// ReSharper disable NotAccessedPositionalProperty.Global -- UI bus event contract; listeners bind the positional members
// ReSharper disable NotAccessedPositionalProperty.Local -- UI bus event contract; listeners bind the positional members
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public record UserLoggedInEvent(string LoginModelUsername) : IUiBusEvent;