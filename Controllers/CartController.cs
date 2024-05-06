using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class CartController : Controller
{
    private readonly DataContext _dataContext;

    public CartController(DataContext db)
    {
        _dataContext = db;
    }

    [Authorize(Roles = "northwind-customer")]
    public IActionResult Index()
    {
        // Retrieve all cart items for the currently logged-in user, including related product details
        var cartItems = _dataContext.CartItems
            .Include(c => c.Product)
            .Where(c => c.Customer.Email == User.Identity.Name)
            .ToList();

        return View(cartItems);
    }

    public IActionResult Remove(int id)
    {
        // Remove the cart item by ID
        var itemToRemove = _dataContext.CartItems.FirstOrDefault(c => c.CartItemId == id);
        if (itemToRemove != null)
        {
            _dataContext.CartItems.Remove(itemToRemove);
            _dataContext.SaveChanges();
        }

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "northwind-customer")]
public IActionResult UpdateQuantity(int id, int quantity)
{
    // Find the cart item
    var cartItem = _dataContext.CartItems.Find(id);
    if (cartItem == null)
    {
        return NotFound();
    }

    if (quantity > 0)
    {
        cartItem.Quantity = quantity;
    }
    else
    {
        // Optionally remove the item if the quantity is set to zero or less
        _dataContext.CartItems.Remove(cartItem);
    }

    _dataContext.SaveChanges();
    return RedirectToAction("Index");
}
   [HttpPost]
[Authorize(Roles = "northwind-customer")]
public IActionResult Checkout()
{
    try
    {
        var items = _dataContext.CartItems
            .Include(c => c.Product)
            .Where(c => c.Customer.Email == User.Identity.Name)
            .ToList();

        if (!items.Any())
        {
            return NotFound("No items in your cart.");
        }

        var customer = _dataContext.Customers.FirstOrDefault(c => c.Email == User.Identity.Name);
        if (customer == null)
        {
            return NotFound("Customer not found.");
        }

        Order newOrder = new Order
        {
            CustomerId = customer.CustomerId,
            OrderDate = DateTime.Now,
            RequiredDate = DateTime.Now.AddDays(7)
        };

        _dataContext.Orders.Add(newOrder);
        _dataContext.SaveChanges();

        foreach (var item in items)
        {
            OrderDetail orderDetail = new OrderDetail
            {
                OrderId = newOrder.OrderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.Product.UnitPrice
            };

            _dataContext.OrderDetails.Add(orderDetail);
        }

        _dataContext.SaveChanges();
        _dataContext.CartItems.RemoveRange(items);
        _dataContext.SaveChanges();

        return RedirectToAction("Index");
    }
    catch (Exception ex)
    {
        // Log the exception here
        // You can use a logging framework or simply print out the message
        // Example: _logger.LogError(ex, "Error processing checkout.");
        return StatusCode(500, "Internal Server Error: " + ex.Message);
    }
}
}
