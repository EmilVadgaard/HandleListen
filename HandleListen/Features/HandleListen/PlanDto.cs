public record PlanTodoDto(int Id, string Title, bool Done, int? DayIndex, int? DayOfMonth, TimeOnly? Time, DateOnly? ResolvedDate, int SortOrder);

public record PlanDto(int Id, string Name, PlanType Type, bool IsActive, DateOnly? ActivatedDate, int? DayCount, int? Month, int? Year, bool IsOwner, string OwnerEmail);

public record PlanDetailDto(PlanDto Plan, List<PlanTodoDto> Todos);

public record CreatePlanRequest(string Name, PlanType Type, int? DayCount, int? Month, int? Year);

public record UpdatePlanRequest(string? Name, int? DayCount, int? Month, int? Year);

public record ActivatePlanRequest(DateOnly ClientToday);

public record CreatePlanTodoRequest(string Title, int? DayIndex, int? DayOfMonth, TimeOnly? Time);

public record UpdatePlanTodoRequest(string Title, bool Done, int? DayIndex, int? DayOfMonth, TimeOnly? Time);

public record ReorderTodosRequest(List<int> TodoIdsInOrder);

public record UpcomingPlanTodoDto(int PlanId, string PlanName, int TodoId, string Title, bool Done, DateOnly Date, TimeOnly? Time);

public record StandardPlanTodoDto(int PlanId, string PlanName, int TodoId, string Title, bool Done);
