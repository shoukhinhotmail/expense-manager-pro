namespace ExpenseManager.Core.Currency;

public record CurrencyInfo(string Code, string Name, string Symbol, int DecimalDigits = 2);
