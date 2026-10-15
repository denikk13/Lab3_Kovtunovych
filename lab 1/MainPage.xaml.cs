namespace lab_1
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("LOG: Головна сторінка зі студентами з'явилася на екрані");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            System.Diagnostics.Debug.WriteLine("LOG: Головна сторінка зникла (відкрили деталі або згорнули)");
        }
    }
}