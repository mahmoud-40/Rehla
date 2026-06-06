using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rehla.Community.DTO.response
{
    public class FollowerDto
    {
        public  string UserId {get;set;} = null!;
        public string Name {get;set;} = null!;
        public string Role {get;set;} = null!;
        public string? AvatarUrl {get;set;}
    }
}