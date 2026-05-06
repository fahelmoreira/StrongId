using System.Text.Json;
using Demo;

// 1. Create a strongly-typed id
var cartId = ShoppingCartId.Create();
Console.WriteLine($"New cart id: {cartId}");

// 2. Round-trip through JSON — the source-generated [JsonConverter] makes ids serialize as flat strings
var cart = new ShoppingCart
{
    Id = cartId,
    Items = ["Shampoo", "Soap", "Toothpaste"]
};

var json = JsonSerializer.Serialize(cart);
Console.WriteLine($"Serialized: {json}");

var roundTrip = JsonSerializer.Deserialize<ShoppingCart>(json)!;
Console.WriteLine($"Round-tripped id: {roundTrip.Id}, items: {string.Join(", ", roundTrip.Items)}");

// 3. Wrong prefix is rejected at deserialization time
var badJson = json.Replace("cart_", "list_");
try
{
    JsonSerializer.Deserialize<ShoppingCart>(badJson);
}
catch (InvalidCastException ex)
{
    Console.WriteLine($"Rejected mismatched prefix: {ex.Message}");
}

// 4. Parsing helpers
const string raw = "cart_019df8fe572c79e186922e1a64fd2bbf";
if (ShoppingCartId.TryParse(raw, out var parsed))
{
    Console.WriteLine($"Parsed cart id: {parsed}");
}

// 5. Cross-type assignment is a compile error — uncomment to see:
// ShoppingListId wrong = cartId;
