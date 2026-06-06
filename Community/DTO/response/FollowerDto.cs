namespace  BreastCancer.Community.DTO.response
{
    public class FollowerDto
    {
        public  string UserId {get;set;} = null!;
        public string Name {get;set;} = null!;
        public string Role {get;set;} = null!;
        public string? AvatarUrl {get;set;}
    }
}