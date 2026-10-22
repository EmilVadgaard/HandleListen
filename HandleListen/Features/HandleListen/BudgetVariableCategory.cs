public class BudgetVariableCategory
{
    public int Id { get; set; }
    public int BudgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public BudgetEntryType Type { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
}
