namespace Console.AI1.Model;

public class CustomBData
{
    public string Name { get; set; } = null!;
    public int ArbitraryNumber { get; set; }
    public DateTimeOffset ArbitraryDate { get; set; }
    public Item[] JobData { get; set; } = [];
}
