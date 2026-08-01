using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpenseManager.Data;

/// <summary>Used by `dotnet ef` at design time only (migrations). The app itself configures the context via DI.</summary>
public class ExpenseManagerDbContextFactory : IDesignTimeDbContextFactory<ExpenseManagerDbContext>
{
    public ExpenseManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ExpenseManagerDbContext>();
        optionsBuilder.UseSqlite("Data Source=expensemanager.design.db");
        return new ExpenseManagerDbContext(optionsBuilder.Options);
    }
}
