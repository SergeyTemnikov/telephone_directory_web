namespace PhoneDirectoryBlazor.Models
{
    public class EmployeeViewModel
    {
        public int? Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public List<string> Positions { get; set; } = new();
        public List<int> PositionIds { get; set; } = new();
        public int? DepartmentId { get; set; }
        public List<string> Phones { get; set; } = new();
        public string Address { get; set; } = "";
        public string Comment { get; set; } = "";
        public int? ManagerId { get; set; }
        public string ManagerDisplayName => Manager?.FullName ?? "-";
        public EmployeeViewModel? Manager { get; set; }
        public List<EmployeeViewModel> Subordinates { get; set; } = new();
    }
}
