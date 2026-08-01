# Expense Manager Pro

A Windows desktop expense tracker built with WinUI 3 / .NET 10 / Entity Framework Core (SQLite).

## Opening the project

1. Double-click `ExpenseManagerPro.slnx` — it opens in Visual Studio.
2. In the toolbar, set the platform dropdown (next to the green Run arrow) to **x64**.
3. Press **F5** (or click the green Run arrow) to build and launch the app.

First launch creates its database automatically at
`%LocalAppData%\ExpenseManagerPro\expensemanager.db`, seeded with a starter set of
expense/income categories.

## Project layout

- `src/ExpenseManager.Core` — domain entities, repository/service interfaces (no dependencies).
- `src/ExpenseManager.Data` — EF Core + SQLite implementation, migrations, seed data.
- `src/ExpenseManager.App` — the WinUI 3 desktop app (MVVM, CommunityToolkit.Mvvm, DI).
- `tests/ExpenseManager.Tests` — xUnit tests for the data/service layer.

## What's implemented (MVP)

- Dashboard with income/expense/balance totals, a bar chart (income vs. expenses) and a
  pie chart (spending by category).
- Expense and Income CRUD (add/edit/delete, search by note).
- Category management (add/edit/delete, separate expense and income categories).
- Light/Dark/System theme, switchable from Settings.
- SQLite persistence via EF Core with an initial migration already generated.

## Not yet built

The full spec (`D:\Expense_Manager_Pro_Claude_Prompt.txt`) also calls for: budgets, savings
goals, a calendar view, recurring transactions, receipt attachments, PDF/Excel/CSV/JSON
export & import, backup/restore, notifications, PIN lock, AES encryption, social-sharing
cards, and MSIX Store packaging. These are intentionally deferred — build the MVP above
first, confirm it feels right, then add features incrementally.

## Running tests

```
dotnet test
```

## Adding a new EF Core migration

After changing an entity in `ExpenseManager.Core.Entities` or the model configuration in
`ExpenseManagerDbContext`:

```
cd src/ExpenseManager.Data
dotnet ef migrations add <MigrationName> --output-dir Migrations
```

(Requires the `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`.)
