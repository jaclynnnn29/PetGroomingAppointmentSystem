using System.ComponentModel.DataAnnotations;

namespace PetGroomingSystem.ViewModels;

public class LoginVM
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    // CAPTCHA
    public string CaptchaQuestion { get; set; } = "";

    public int CaptchaAnswer { get; set; }

    [Required(ErrorMessage = "Please enter the correct CAPTCHA answer :) :) ")]
    public int? CaptchaUserAnswer { get; set; }
}