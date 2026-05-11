// ViewModels/CreateDepartementRequest.cs
namespace Administration.ViewModels
{
    public class CreateDepartementRequest
    {
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
