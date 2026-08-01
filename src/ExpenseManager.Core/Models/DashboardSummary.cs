namespace ExpenseManager.Core.Models;

public class CategoryTotal
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Color { get; set; } = "#6B7280";
    public decimal Total { get; set; }
}

public class DashboardSummary
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance => TotalIncome - TotalExpense;
    public List<CategoryTotal> ExpenseByCategory { get; set; } = new();
}
