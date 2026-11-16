using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

public class WhatDoIHaveController : ControllerBase
{
    [HttpGet("/_diagnostics/services")]
    public string Services([FromServices] RegisteredServices registeredServices)
    {
        return registeredServices.Describe();
    }
}
