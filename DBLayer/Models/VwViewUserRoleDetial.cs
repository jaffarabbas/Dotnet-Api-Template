using System;
using System.Collections.Generic;

namespace DBLayer.Models;

public partial class VwViewUserRoleDetial
{
    public long UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string RoleTitle { get; set; } = null!;

    public string ResourceName { get; set; } = null!;

    public string ActionTypeTitle { get; set; } = null!;
}
