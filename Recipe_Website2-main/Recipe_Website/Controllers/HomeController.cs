using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecipeApp.Api.Models;

public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public HomeController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public IActionResult Index()
    {
        return View();
    }


    // GET: Register
    public IActionResult Register() => View();

    // POST: Register
    [HttpPost]
    public async Task<IActionResult> Register(string displayName, string email, string password)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError("", err.Description);
            }
            return View();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index");
    }

    // GET: Login
    public IActionResult Login() => View();

    // POST: Login
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var result = await _signInManager.PasswordSignInAsync(email, password, false, false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View();
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index");
    }

}
