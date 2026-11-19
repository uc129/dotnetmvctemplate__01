using Microsoft.AspNetCore.Mvc;

namespace AppTemplate.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction(nameof(CustomersController.Index), "Customers");
}
