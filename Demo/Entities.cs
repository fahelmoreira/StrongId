namespace Demo;

public class ShoppingCart
{
    public required ShoppingCartId Id { get; set; }
    public required List<string> Items { get; set; }
}

public class Product
{
    public required ProductId Id { get; set; }
    public required string Name { get; set; }
}

public class Customer
{
    public required CustomerId Id { get; set; }
    public required string Name { get; set; }
}

public class Order
{
    public required OrderId Id { get; set; }
    public required string Description { get; set; }
}

public class ShoppingList
{
    public required ShoppingListId Id { get; set; }
    public required string Title { get; set; }
}
