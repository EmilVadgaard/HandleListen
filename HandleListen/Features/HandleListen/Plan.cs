public enum PlanType
{
    Standard,
    Weekly,
    Monthly
}

public class Plan
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public required string Name { get; set; }
    public PlanType Type { get; set; }
    public bool IsActive { get; set; } = false;

    // Weekly only - "day 0" is the day after activation; later days follow from it.
    public DateOnly? ActivatedDate { get; set; }

    // Weekly only - number of days in the plan, locked once activated.
    public int? DayCount { get; set; }

    // Monthly only - the chosen month/year, locked once activated.
    public int? Month { get; set; }
    public int? Year { get; set; }
}
