using Microsoft.AspNetCore.Http;

namespace PetGroomingSystem;

public static class Extensions
{
    public static bool IsAjax(this HttpRequest request)
    {
        return request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}