public enum BudgetEntryType
{
    Income,
    Expense
}

public class BudgetFixedEntry
{
    public int Id { get; set; }
    public int BudgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BudgetEntryType Type { get; set; }
    public decimal Amount { get; set; }
}
