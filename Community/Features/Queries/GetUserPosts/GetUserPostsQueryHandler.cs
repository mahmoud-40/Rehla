using System.Globalization;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using BreastCancer.Community.DTO.response;
using BreastCancer.Community.Exceptions;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using BreastCancer.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BreastCancer.Community.Features.Queries.GetUserPosts;

public class GetUserPostsQueryHandler : IRequestHandler<GetUserPostsQuery, UserPostsResponseDto>
{
    private readonly BreastCancerDB _context;
    private readonly IPostVisibilityService _postVisibilityService;
    private readonly IMapper _mapper;

    public GetUserPostsQueryHandler(
        BreastCancerDB context, 
        IPostVisibilityService postVisibilityService,
        IMapper mapper)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _postVisibilityService = postVisibilityService ?? throw new ArgumentNullException(nameof(postVisibilityService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<UserPostsResponseDto> Handle(GetUserPostsQuery request, CancellationToken cancellationToken)
    {
        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            
        if (!userExists)
        {
            throw new NotFoundException($"User with ID '{request.UserId}' was not found");
        }
        
        int limit = Math.Clamp(request.Limit, 1, 50);
        
        IQueryable<Post> postsQuery = _context.Posts
            .AsNoTracking()
            .Include(p => p.Author)
                .ThenInclude(a => a.Patient)
            .Include(p => p.Author)
                .ThenInclude(a => a.Doctor)
            .Include(p => p.Author)
                .ThenInclude(a => a.Caregiver)
            .Include(p => p.Reactions)
            .Include(p => p.Comments.Where(c => !c.IsDeleted))
            .Where(p => p.AuthorId == request.UserId)
            .Where(p => !p.IsDeleted)  
            .OrderByDescending(p => p.CreatedAt);

        bool isViewingOwnPosts = request.CurrentUserId == request.UserId;

        if (!isViewingOwnPosts)
        {
            postsQuery = await _postVisibilityService
                .ApplyVisibilityFilterAsync(postsQuery, request.CurrentUserId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(request.Cursor) && 
            DateTimeOffset.TryParseExact(request.Cursor, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var cursorDate))
        {
            var cursorUtc = cursorDate.UtcDateTime;
            postsQuery = postsQuery.Where(p => p.CreatedAt < cursorUtc);
        }

        var totalCount = await postsQuery.CountAsync(cancellationToken);
        
        var postsData = await postsQuery
            .Take(limit + 1)
            .ProjectTo<PostDTO>(_mapper.ConfigurationProvider, new { currentUserId = request.CurrentUserId ?? string.Empty })
            .ToListAsync(cancellationToken);
        
        string? nextCursor = null;
        bool hasMorePosts = postsData.Count > limit;

        if (hasMorePosts)
        {
            nextCursor = new DateTimeOffset(postsData[limit - 1].CreatedAt, TimeSpan.Zero).ToString("o");
            postsData.RemoveAt(limit);
        }

        return new UserPostsResponseDto
        {
            Posts = postsData,
            NextCursor = nextCursor,
            Total = totalCount
        };
    }
}