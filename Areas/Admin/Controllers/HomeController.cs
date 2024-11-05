using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Route("admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : BaseAdminController
	{
        public HomeController(ApplicationDbContext context) : base(context)
        {
        }

        [Route("")]
		[Route("Index")]
		public IActionResult Index()
		{
		
			return View();
		}
	}
}
