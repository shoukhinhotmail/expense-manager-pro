using CommunityToolkit.Mvvm.ComponentModel;

namespace ExpenseManager.App.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;
}
