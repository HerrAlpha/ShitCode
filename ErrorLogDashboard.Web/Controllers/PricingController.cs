using Microsoft.AspNetCore.Mvc;

namespace ErrorLogDashboard.Web.Controllers;

public class PricingController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
