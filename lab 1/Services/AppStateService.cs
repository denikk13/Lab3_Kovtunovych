namespace lab_1.Services
{
    public interface IAppStateService
    {
        string LastAction { get; set; }
    }

    public class AppStateService : IAppStateService
    {
        public string LastAction { get; set; } = "Запуск програми";
    }
}