using Microsoft.AspNetCore.Mvc;

namespace Tech_Store.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Route("admin")]
	public class HomeController : Controller
	{
		[Route("")]
		[Route("Index")]
		public IActionResult Index()
		{
			return View();
		}
	}
}
