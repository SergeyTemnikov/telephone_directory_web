namespace PhoneDirectoryBlazor.Models
{
    public class UserViewModel
    {
        public int? Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime? EndDateOfAccess { get; set; }
        public bool IsActive { get; set; }
        public List<RoleViewModel> Roles { get; set; } = new();
    }

    public class RoleViewModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
