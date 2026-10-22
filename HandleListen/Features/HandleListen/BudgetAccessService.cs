using Microsoft.EntityFrameworkCore;

public class BudgetAccessService
{
    private readonly AppDbContext _context;

    public BudgetAccessService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetOrCreateBudgetIdAsync(string userId)
    {
        var existingBudgetId = await _context.BudgetMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.BudgetId)
            .FirstOrDefaultAsync();
        if (existingBudgetId != 0) return existingBudgetId;

        var budget = new Budget();
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        _context.BudgetMembers.Add(new BudgetMember { BudgetId = budget.Id, UserId = userId });
        await _context.SaveChangesAsync();

        return budget.Id;
    }
}
