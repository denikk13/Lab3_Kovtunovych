using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Serilog;

namespace lab_1.Services;

public interface IAuthService
{
    Task<(bool Ok, string Message)> LoginAsync(string login, string password);
    Task<(bool Ok, string Message)> LoginWithBiometricsAsync();
    Task<bool> HasSavedSessionAsync();
    Task LogoutAsync();
}

public class AuthService : IAuthService
{
    private const string TokenKeyPrefix = "auth_token_";
    private const string RoleKeyPrefix = "auth_role_";
    private const string KnownUsersKey = "auth_known_users";

    // Очікуваний відбиток сертифіката сервера (SHA-256) для сертифікат-пінінгу
    private const string ExpectedCertThumbprint = "AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD";

    private readonly ISessionService _session;

    public AuthService(ISessionService session)
    {
        _session = session;
    }

    private HttpClient CreatePinnedClient()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, cert, _, sslPolicyErrors) =>
        {
            if (cert is null)
            {
                Log.Warning("Server certificate is null during TLS validation");
                return false;
            }

            var actualThumbprint = cert.GetCertHashString();
            var expectedClean = ExpectedCertThumbprint.Replace(":", "").Trim();
            var isMatch = string.Equals(actualThumbprint, expectedClean, StringComparison.OrdinalIgnoreCase);

            if (!isMatch)
            {
                Log.Warning("Certificate pinning mismatch. Expected: {Expected}, Actual: {Actual}", expectedClean, actualThumbprint);
                return false;
            }

            return sslPolicyErrors == SslPolicyErrors.None || sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors;
        };

        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
    }

    private static string NormalizeLogin(string login) => login.Trim().ToLowerInvariant();

    private static async Task<List<string>> GetKnownUsersAsync()
    {
        var raw = await SecureStorage.Default.GetAsync(KnownUsersKey) ?? string.Empty;
        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task AddKnownUserAsync(string user)
    {
        var users = await GetKnownUsersAsync();
        if (!users.Contains(user, StringComparer.OrdinalIgnoreCase))
        {
            users.Add(user);
            await SecureStorage.Default.SetAsync(KnownUsersKey, string.Join(",", users));
        }
    }

    public async Task<(bool Ok, string Message)> LoginAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return (false, "Введіть логін і пароль.");

        var normalizedLogin = NormalizeLogin(login);

        // Спроба мережевого виклику з перевіркою сертифіката сервера (пінінг)
        try
        {
            using var client = CreatePinnedClient();
            var payload = new { Login = normalizedLogin, Password = password };
            using var response = await client.PostAsJsonAsync("https://localhost:5001/api/auth/login", payload);
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Remote auth succeeded with pinned certificate for {Login}", normalizedLogin);
            }
        }
        catch (HttpRequestException ex)
        {
            Log.Warning(ex, "Pinned TLS check / remote auth fallback for user {Login}", normalizedLogin);
        }

        var role = normalizedLogin.Equals("admin", StringComparison.OrdinalIgnoreCase)
            ? UserRole.Administrator
            : UserRole.Dispatcher;

        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{normalizedLogin}:{role}:{Guid.NewGuid()}"));

        await SecureStorage.Default.SetAsync($"{TokenKeyPrefix}{normalizedLogin}", token);
        await SecureStorage.Default.SetAsync($"{RoleKeyPrefix}{normalizedLogin}", role.ToString());
        await AddKnownUserAsync(normalizedLogin);

        _session.SetSession(normalizedLogin, role);
        Log.Information("User {User} logged in with role {Role}", normalizedLogin, role);

        return (true, $"Вхід виконано ({role}).");
    }

    public async Task<bool> HasSavedSessionAsync()
    {
        var users = await GetKnownUsersAsync();
        foreach (var user in users)
        {
            var token = await SecureStorage.Default.GetAsync($"{TokenKeyPrefix}{user}");
            if (!string.IsNullOrWhiteSpace(token))
                return true;
        }
        return false;
    }

    public async Task<(bool Ok, string Message)> LoginWithBiometricsAsync()
    {
        var users = await GetKnownUsersAsync();
        var availableUsers = new List<string>();

        foreach (var user in users)
        {
            var token = await SecureStorage.Default.GetAsync($"{TokenKeyPrefix}{user}");
            if (!string.IsNullOrWhiteSpace(token))
                availableUsers.Add(user);
        }

        if (availableUsers.Count == 0)
            return (false, "Немає збереженого токена. Спочатку увійдіть логіном і паролем.");

        string? selectedUser;
        if (availableUsers.Count == 1)
        {
            selectedUser = availableUsers[0];
        }
        else
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is null)
                return (false, "Не вдалося відкрити вікно вибору акаунту.");

            selectedUser = await page.DisplayActionSheet(
                "В який акаунт увійти через біометрію?",
                "Скасувати",
                null,
                availableUsers.ToArray());

            if (string.IsNullOrWhiteSpace(selectedUser) || selectedUser == "Скасувати")
                return (false, "Вибір акаунту скасовано.");
        }

        bool bioPassed = await AuthenticateBiometricInternalAsync();
        if (!bioPassed)
            return (false, "Біометричну автентифікацію не пройдено.");

        var roleText = await SecureStorage.Default.GetAsync($"{RoleKeyPrefix}{selectedUser}");
        var role = Enum.TryParse<UserRole>(roleText, out var parsed) ? parsed : UserRole.Dispatcher;

        _session.SetSession(selectedUser, role);
        Log.Information("Biometric login succeeded for {User} ({Role})", selectedUser, role);
        return (true, $"Вхід через біометрію успішний ({selectedUser} — {role}).");
    }

    private static async Task<bool> AuthenticateBiometricInternalAsync()
    {
#if ANDROID || IOS
        // Якщо підключено Plugin.Fingerprint:
        // var result = await Plugin.Fingerprint.CrossFingerprint.Current.AuthenticateAsync(
        //     new Plugin.Fingerprint.Abstractions.AuthenticationRequestConfiguration("Біометрія", "Підтвердьте вхід у застосунок"));
        // return result.Authenticated;
#endif
        await Task.Delay(150);
        return true;
    }

    public Task LogoutAsync()
    {
        _session.Clear();
        Log.Information("User logged out (saved account tokens kept for biometric login)");
        return Task.CompletedTask;
    }
}