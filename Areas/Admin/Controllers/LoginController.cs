using Microsoft.AspNetCore.Mvc;

namespace Tech_Store.Areas.Admin.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
