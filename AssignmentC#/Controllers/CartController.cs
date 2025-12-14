using Microsoft.AspNetCore.Mvc;
using AssignmentC_.Models;

namespace AssignmentC_.Controllers;

public class CartController : Controller
{
    private readonly DB db;

    public CartController(DB db)
    {
        this.db = db;
    }

    // GET: /Cart or /Cart/Index
    [HttpGet]
    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    // POST: /Cart/RemoveItem
    [HttpPost]
    public IActionResult RemoveItem(int index)
    {
        var cart = GetCart();

        if (index >= 0 && index < cart.Items.Count)
        {
            cart.Items.RemoveAt(index);
            SaveCart(cart);
            TempData["Success"] = "Item removed from cart";
        }

        return RedirectToAction("Index");
    }

    // POST: /Cart/Clear
    [HttpPost]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove("CART");
        TempData["Success"] = "Cart cleared";
        return RedirectToAction("Index");
    }

    // GET: /Cart/Checkout
    [HttpGet]
    public IActionResult Checkout()
    {
        var cart = GetCart();

        if (!cart.Items.Any())
        {
            TempData["Error"] = "Your cart is empty";
            return RedirectToAction("Index");
        }

        // TODO: Implement checkout logic
        return View(cart);
    }

    private CartViewModel GetCart()
    {
        try
        {
            var json = HttpContext.Session.GetString("CART");

            if (string.IsNullOrEmpty(json))
            {
                return new CartViewModel();
            }

            var cart = System.Text.Json.JsonSerializer.Deserialize<CartViewModel>(json);
            return cart ?? new CartViewModel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetCart: {ex.Message}");
            return new CartViewModel();
        }
    }

    private void SaveCart(CartViewModel cart)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("CART", json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SaveCart: {ex.Message}");
            throw;
        }
    }
}