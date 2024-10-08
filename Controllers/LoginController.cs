using Microsoft.AspNetCore.Mvc;

namespace Tech_Store.Controllers
{
    [Route("Login")]
    public class LoginController : Controller
    {
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            return View();
        }
        [Route("/Register")]
        public IActionResult Register()
        {
            return View();
        }
        [Route("/ResetPassWord")]
        public IActionResult ResetPassword()
        {
            return View();
        }
    }
}
