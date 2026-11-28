using lab_1.ViewModels;

namespace lab_1;

public partial class MainPage : ContentPage
{
    private readonly StudentViewModel _viewModel;

    public MainPage(StudentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshRoleState();
    }
}