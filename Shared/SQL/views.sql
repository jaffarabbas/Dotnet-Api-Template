create view VwViewUserRoleDetial
as
select 
    u.UserID,
    u.UserName,
    r.RoleTitle,
    res.ResourceName,
    at.ActionTypeTitle
from tblUsers u
join tblUserRole ur on u.UserID = ur.UserID
join tblRole r on ur.RoleID = r.RoleID
join tblRolePermission rp on r.RoleID = rp.RoleID
join tblPermission p on rp.PermissionID = p.PermissionID
join tblResource res on p.ResourceID = res.ResourceID
join tblActionType at on p.ActionTypeID = at.ActionTypeID
go