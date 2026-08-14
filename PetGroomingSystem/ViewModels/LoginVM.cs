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
}