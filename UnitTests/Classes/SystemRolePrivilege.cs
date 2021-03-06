using System;
using System.Collections.Generic;
using TinySql.Attributes;

namespace TinySql.Classes
{	public partial class SystemRolePrivilege
{
		[PK]
		public Decimal  SystemRolePrivilegeID { get; set; }

		public Int32  CanCreate { get; set; }

		public Int32  CanDelete { get; set; }

		public Int32  CanExecute { get; set; }

		public Int32  CanRead { get; set; }

		public Int32  CanWrite { get; set; }

		public Decimal  CreatedBy { get; set; }

		public DateTime  CreatedOn { get; set; }

		public Decimal  ModifiedBy { get; set; }

		public DateTime  ModifiedOn { get; set; }

		[FK("SystemPrivilege","SystemPrivilegeID","dbo","RefSystemPrivilege7")]
		public Decimal  SystemPrivilegeID { get; set; }

		[FK("SystemRole","SystemRoleID","dbo","RefSystemRole6")]
		public Decimal  SystemRoleID { get; set; }

	}
}
