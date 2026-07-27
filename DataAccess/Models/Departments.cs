namespace DataAccess.Models;

public class Departments
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;


   
    public int? ParentDepartmentId { get; set; }

    public Departments? ParentDepartment { get; set; }

    public ICollection<Departments> SubDepartments { get; set; }
        = new List<Departments>();

    // المستخدمون الذين قسمهم الأساسي هو هذا القسم
    public ICollection<Users> Users { get; set; }
        = new List<Users>();
}