namespace PhoneDirectoryBlazor.Models
{
    public class DepartmentNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? ParentId { get; set; }
        public List<DepartmentNode> Children { get; set; } = new();
        public List<EmployeeViewModel> Employees { get; set; } = new();
        public bool IsCollapsed { get; set; } = false;
    }
}
