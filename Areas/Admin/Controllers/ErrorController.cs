using Microsoft.AspNetCore.Mvc;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Error")]
    public class ErrorController : BaseAdminController
    {
        public ErrorController(ApplicationDbContext context) : base(context)
        {
        }

        [HttpGet("404")]
        public IActionResult NotFoundPage()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound");
        }
    }
}
