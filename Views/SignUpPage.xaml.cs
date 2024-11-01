using LoginWithFirebase.ViewModel;

namespace LoginWithFirebase.Views;

public partial class SignUpPage : ContentPage
{
    private readonly SignUpViewModel _signUpViewModel;

    

    public SignUpPage(SignUpViewModel signUpViewModel )
	{
		InitializeComponent();
        BindingContext= _signUpViewModel = signUpViewModel;
    }
}