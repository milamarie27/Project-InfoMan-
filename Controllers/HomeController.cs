using Microsoft.AspNetCore.Mvc;
using OnlineClearanceSystem.Models;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        bool canConnect = _context.Database.CanConnect();

        ViewBag.ConnectionStatus = canConnect ? "CONNECTED ✅" : "NOT CONNECTED ❌";

        return View();
    }
}