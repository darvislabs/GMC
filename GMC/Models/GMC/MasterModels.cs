namespace GMC.Models.GMC
{
    public class UserRoleModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
    }

    public class FormPermissionModel
    {
        public int PermissionId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string FormName { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
    }
}
