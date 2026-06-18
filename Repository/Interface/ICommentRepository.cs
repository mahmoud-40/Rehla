using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BreastCancer.Models;
using BreastCancer.Repository.Interface;

namespace Rehla.Repository.Interface
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        Task<Comment> AddCommentAsync(Comment comment);
        Task<Comment?> GetByIdWithIncludesAsync(int commentId);
        Task SoftDelete(int commentId);
    }
}