namespace lab_1
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Відновлюємо тему
            var savedTheme = Preferences.Get("AppTheme", "Light");
            Application.Current.UserAppTheme = savedTheme == "Dark" ? AppTheme.Dark : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            window.Stopped += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("LOG: Застосунок згорнуто у фон (Stopped)");
                Preferences.Set("last_active_time", DateTime.Now.ToString());
            };

            window.Resumed += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("LOG: Застосунок знову відкрито (Resumed)");
            };

            window.Destroying += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("LOG: Застосунок закривається назавжди (Destroying)");
            };

            return window;
        }
    }
}