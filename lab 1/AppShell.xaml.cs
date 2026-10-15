
namespace lab_1
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();


            Routing.RegisterRoute("studentdetail", typeof(Views.StudentDetailPage));
        }
    }
}
