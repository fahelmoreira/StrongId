using System.Text.Json;
using Demo;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StrongId.Configuration;

// ──────────────────────────────────────────────────────────────────
// 1. Global defaults — applied to any [StrongIdPrefix] that doesn't
//    specify an IdScheme. Attribute values still override this
//    (see ProductId / CustomerId below).
// ──────────────────────────────────────────────────────────────────
StrongIdDefaults.Configure(c =>
{
    c.IdScheme = IdScheme.Uuid7;             // applies to ShoppingListId
    c.StorageFormat = StorageFormat.Native;  // EF picks Guid vs string per IdScheme
});

// ──────────────────────────────────────────────────────────────────
// 2. Generate one id of each flavour and show its shape.
// ──────────────────────────────────────────────────────────────────
var listId  = ShoppingListId.Create();   // resolves to SequenceString via the global config
var prodId  = ProductId.Create();        // Uuid7 — hex
var custId  = CustomerId.Create();       // Uuid4 — hex
var cartId  = ShoppingCartId.Create();   // SequenceString — Crockford base32
var orderId = OrderId.Create();          // Uuid7 + StorageFormat.String
var sessId  = SessionId.Create();        // SequenceString + per-type salt

Console.WriteLine("── Id generation ──");
PrintId("ShoppingListId  (global default)", listId.Value);
PrintId("ProductId       (Uuid7)         ", prodId.Value);
PrintId("CustomerId      (Uuid4)         ", custId.Value);
PrintId("ShoppingCartId  (SequenceString)", cartId.Value);
PrintId("OrderId         (Uuid7 + String)", orderId.Value);
PrintId("SessionId       (Salted Seq)    ", sessId.Value);

// ──────────────────────────────────────────────────────────────────
// 3. JSON round-trip — source-generated converter flattens to a string.
// ──────────────────────────────────────────────────────────────────
Console.WriteLine("\n── JSON round-trip ──");
var cart = new ShoppingCart { Id = cartId, Items = ["Shampoo", "Soap", "Toothpaste"] };
var json = JsonSerializer.Serialize(cart);
Console.WriteLine($"Serialized:    {json}");
var roundTrip = JsonSerializer.Deserialize<ShoppingCart>(json)!;
Console.WriteLine($"Deserialized:  id={roundTrip.Id}, items=[{string.Join(", ", roundTrip.Items)}]");

// ──────────────────────────────────────────────────────────────────
// 4. Wrong prefix is rejected at deserialization.
// ──────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Prefix validation ──");
try
{
    JsonSerializer.Deserialize<ShoppingCart>(json.Replace("cart_", "list_"));
}
catch (InvalidCastException ex)
{
    Console.WriteLine($"Rejected mismatched prefix: {ex.Message}");
}

// ──────────────────────────────────────────────────────────────────
// 5. Parsing helpers.
// ──────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Parsing ──");
if (ShoppingCartId.TryParse(cartId.Value, out var parsed))
{
    Console.WriteLine($"Parsed cart id: {parsed}");
}
Console.WriteLine($"TryParse rubbish: {ShoppingCartId.TryParse("cart_not-valid", out _)}");

// ──────────────────────────────────────────────────────────────────
// 5b. Salted SequenceString — suffix carries a 16-bit signature
//     derived from the id's type, so re-prefixed strings are rejected.
// ──────────────────────────────────────────────────────────────────
Console.WriteLine("\n── Salted SequenceString ──");
Console.WriteLine($"Session id parses back: {SessionId.TryParse(sessId.Value, out _)}");

// Build an 18-char base32 suffix that's well-formed for an unsalted
// SequenceString but won't match the SessionId salt signature.
var forged = $"sess_{ShoppingCartId.Create().Value.Split('_')[1]}";
Console.WriteLine($"Forged session id parses back: {SessionId.TryParse(forged, out _)}  (expected False)");

// ──────────────────────────────────────────────────────────────────
// 6. EF Core + HasAutoConversion — picks UUID or string storage
//    automatically based on each id's IdScheme / StorageFormat.
// ──────────────────────────────────────────────────────────────────
Console.WriteLine("\n── EF Core auto-conversion (SQLite in-memory) ──");

await using var conn = new SqliteConnection("Data Source=:memory:");
await conn.OpenAsync();

var options = new DbContextOptionsBuilder<DemoDbContext>().UseSqlite(conn).Options;

await using (var ctx = new DemoDbContext(options))
{
    await ctx.Database.EnsureCreatedAsync();
    ctx.Products.Add(new Product   { Id = prodId,  Name = "Widget" });
    ctx.Customers.Add(new Customer { Id = custId,  Name = "Acme Co" });
    ctx.Orders.Add(new Order       { Id = orderId, Description = "Pallet of widgets" });
    ctx.Lists.Add(new ShoppingList { Id = listId,  Title = "Groceries" });
    await ctx.SaveChangesAsync();
}

await PrintStoredColumn(conn, "Products",  "Id", "(Uuid7        → stored as Guid)");
await PrintStoredColumn(conn, "Customers", "Id", "(Uuid4        → stored as Guid)");
await PrintStoredColumn(conn, "Orders",    "Id", "(Uuid7+String → stored as prefixed string)");
await PrintStoredColumn(conn, "Lists",     "Id", "(SequenceStr  → stored as prefixed string)");

await using (var ctx = new DemoDbContext(options))
{
    var loaded = await ctx.Products.SingleAsync();
    Console.WriteLine($"\nRound-trip from DB: {loaded.Id} (matches generated? {loaded.Id == prodId})");
}

return;

static void PrintId(string label, string value)
    => Console.WriteLine($"  {label}: {value}  ({value.Split('_')[1].Length} chars)");

static async Task PrintStoredColumn(SqliteConnection conn, string table, string column, string note)
{
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = $"SELECT \"{column}\" FROM \"{table}\" LIMIT 1";
    var raw = (string?)await cmd.ExecuteScalarAsync();
    Console.WriteLine($"  {table,-10} {column} = {raw,-40} {note}");
}
