using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MyBGList.Constants;

namespace MyBGList.gRPC;

public class GrpcService : Grpc.GrpcBase
{
    private readonly ApplicationDbContext _context;

    public GrpcService(ApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task<BoardGameResponse> GetBoardGame(BoardGameRequest request, ServerCallContext context)
    {
        var boardgame = await _context.BoardGames.FindAsync(request.Id);
        var response = new BoardGameResponse();

        if (boardgame != null)
        {
            response.Id = boardgame.Id;
            response.Name = boardgame.Name;
            response.Year = boardgame.Year;
        }

        return response;
    }

    [Authorize(Roles = nameof(RoleNames.Moderator))]
    public override async Task<BoardGameResponse> UpdateBoardGame(
        UpdateBoardGameRequest request,
        ServerCallContext context)
    {
        var boardgame = await _context.BoardGames.FindAsync(request.Id);
        var response = new BoardGameResponse();

        if (boardgame != null)
        {
            boardgame.Name = request.Name;
            boardgame.LastModifiedDate = DateTime.Now;
            _context.BoardGames.Update(boardgame);
            await _context.SaveChangesAsync();
            response.Id = boardgame.Id;
            response.Name = boardgame.Name;
            response.Year = boardgame.Year;
        }

        return response;
    }
}