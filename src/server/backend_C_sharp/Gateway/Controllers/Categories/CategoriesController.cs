using Core.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers.Categories;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    /// <summary>
    /// Возвращает все категории. Статично, можно кешировать.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(IReadOnlyList<Category>), StatusCodes.Status200OK)]
    public IReadOnlyList<Category> GetAllCategories()
    {
        return Category.All;
    }
}