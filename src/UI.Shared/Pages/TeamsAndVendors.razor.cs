using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/admin/teams")]
[Authorize]
public partial class TeamsAndVendors : AppComponentBase;
