namespace AssignmentC_.Models;

public class CartViewModel
{
    public List<CartItem> Items { get; set; } = new();

    public decimal GrandTotal => Items.Sum(i => i.TotalPrice);
}
