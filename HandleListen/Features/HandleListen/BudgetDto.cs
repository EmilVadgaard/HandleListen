public record BudgetIdDto(int Id);

public record BudgetMemberDto(string UserId, string Email);

public record BudgetInviteDto(int Id, string FromEmail, string ToEmail, bool IsIncoming, DateTime CreatedAt);

public record BudgetFixedEntryDto(int Id, string Name, BudgetEntryType Type, decimal Amount);

public record CreateBudgetFixedEntryRequest(string Name, BudgetEntryType Type, decimal Amount);

public record UpdateBudgetFixedEntryRequest(string Name, BudgetEntryType Type, decimal Amount);

public record BudgetVariableCategoryDto(int Id, string Name, BudgetEntryType Type, decimal ExpectedAmount, decimal ActualAmount);

public record CreateBudgetVariableCategoryRequest(string Name, BudgetEntryType Type, decimal ExpectedAmount, decimal ActualAmount);

public record UpdateBudgetVariableCategoryRequest(string Name, BudgetEntryType Type, decimal ExpectedAmount, decimal ActualAmount);
