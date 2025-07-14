using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace POS_Shoes.Areas.Manager.Helpers
{
    public class ManagerAuthorizationAttribute : AuthorizeAttribute
    {
        public ManagerAuthorizationAttribute()
        {
            Roles = "Manager";
        }
    }
}