using Microsoft.AspNetCore.Mvc;
using Tech_Store.Models;
using Tech_Store.Services;
using Tech_Store.Services.Admin.NotificationServices;

namespace Tech_Store.Controllers
{
    public class ErrorController : BaseController
    {
        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;

        public ErrorController(IConfiguration configuration, NotificationService notificationService, ApplicationDbContext context) : base(context)
        {
            _configuration = configuration;
            _notificationService = notificationService;
        }

        [Route("404")]
        public IActionResult NotFoundPage()
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }

        [Route("500")]
        public IActionResult ServerError()
        {
            Response.StatusCode = 500;
            return View("Error");
        }
    }
}
