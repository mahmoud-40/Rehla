using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BreastCancer.Context;
using BreastCancer.Models;
using BreastCancer.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Rehla.Repository.Interface;

namespace Rehla.Repository.Repositories
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        public CommentRepository(BreastCancerDB _Context) : base(_Context)
        {
        }

        public async Task<Comment> AddCommentAsync(Comment comment)
        {
            if(comment != null)
            {
                comment.CreatedAt = DateTime.UtcNow;
                comment.IsDeleted = false;

                await _Context.AddAsync(comment);

                return comment;
            }
            return null;
        }

        public async Task SoftDelete(int commentId)
        {
            var comment = await _Context.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
            if(comment != null)
            {
                comment.IsDeleted = true;
                _Context.Update(comment);
            }
        }
    }
}