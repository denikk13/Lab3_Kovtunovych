using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using lab_1.Services;
using lab_1.Views;

namespace lab_1.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _authService;
    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _hasSavedToken;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new Command(async () => await OnLoginAsync());
        BiometricLoginCommand = new Command(async () => await OnBiometricLoginAsync());
    }

    public string Login
    {
        get => _login;
        set { _login = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool HasSavedToken
    {
        get => _hasSavedToken;
        set { _hasSavedToken = value; OnPropertyChanged(); }
    }

    public ICommand LoginCommand { get; }
    public ICommand BiometricLoginCommand { get; }

    public async Task CheckSavedTokenAsync()
    {
        HasSavedToken = await _authService.HasSavedSessionAsync();
    }

    private async Task OnLoginAsync()
    {
        var result = await _authService.LoginAsync(Login, Password);
        StatusMessage = result.Message;

        if (!result.Ok)
        {
            await Shell.Current.DisplayAlert("Помилка входу", result.Message, "ОК");
            return;
        }

        HasSavedToken = true;
        await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
    }

    private async Task OnBiometricLoginAsync()
    {
        var result = await _authService.LoginWithBiometricsAsync();
        StatusMessage = result.Message;

        if (!result.Ok)
        {
            await Shell.Current.DisplayAlert("Біометрія", result.Message, "ОК");
            return;
        }

        await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}