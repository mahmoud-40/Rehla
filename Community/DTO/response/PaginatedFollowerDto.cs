using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BreastCancer.Community.DTO.response;

namespace Rehla.Community.DTO.response
{
    public class PaginatedFollowerDto
    {
        public List<FollowerDto> Followers {get;set;} = new();
        public string? NextCursor {get;set;} 
        public int Total {get;set;}
    }
}