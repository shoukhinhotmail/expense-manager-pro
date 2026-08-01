using ExpenseManager.Core.Entities;
using ExpenseManager.Core.Repositories;

namespace ExpenseManager.App.ViewModels;

public partial class ExpensesViewModel(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IWalletRepository walletRepository)
    : TransactionListViewModel(transactionRepository, categoryRepository, walletRepository, TransactionType.Expense);
