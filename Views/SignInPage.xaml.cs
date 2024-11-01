using LoginWithFirebase.ViewModel;

namespace LoginWithFirebase.Views;

public partial class SignInPage : ContentPage
{
    private readonly SignInViewModel _viewModel;

    public SignInPage(SignInViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    private async void OnSignInClicked(object sender, EventArgs e)
    {
        
        await _viewModel.SignIn(this);
    }
}