using FoodieRadar.Api.Dtos;
using FoodieRadar.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodieRadar.Api.Controllers;

[ApiController]
[Route("api/meta")]
public class MetaController : ControllerBase
{
    /// <summary>Filter options for the frontend (cuisines, tastes, areas, reviewer origins).</summary>
    [HttpGet]
    public MetaDto Get() => new(
        CuisineCatalog.Cuisines,
        CuisineCatalog.Tastes,
        CuisineCatalog.Areas,
        CuisineCatalog.Origins);
}
