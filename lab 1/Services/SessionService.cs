namespace lab_1.Services;

public enum UserRole
{
    Dispatcher,
    Administrator
}

public interface ISessionService
{
    UserRole? CurrentRole { get; }
    string? CurrentUser { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(UserRole role);
    void SetSession(string user, UserRole role);
    void Clear();
}

public class SessionService : ISessionService
{
    public UserRole? CurrentRole { get; private set; }
    public string? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentRole.HasValue;

    public bool IsInRole(UserRole role) => CurrentRole == role;

    public void SetSession(string user, UserRole role)
    {
        CurrentUser = user;
        CurrentRole = role;
    }

    public void Clear()
    {
        CurrentUser = null;
        CurrentRole = null;
    }
}