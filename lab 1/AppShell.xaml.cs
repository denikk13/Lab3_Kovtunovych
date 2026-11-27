using Serilog;

namespace lab_1
{
    public partial class AppShell : Shell
    {
        private readonly ILogger _logger = Log.ForContext<AppShell>();

        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("studentdetail", typeof(Views.StudentDetailPage));
            _logger.Information("AppShell initialized and route 'studentdetail' registered");
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);
            _logger.Debug("Shell navigating to {Target} (Source: {Source})",
                args.Target?.Location, args.Source);
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);
            _logger.Information("Shell navigated to {Location}", args.Current?.Location);
        }
    }
}