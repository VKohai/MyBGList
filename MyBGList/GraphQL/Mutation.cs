using HotChocolate.Authorization;
using MyBGList.Constants;
using MyBGList.DTO;

namespace MyBGList.GraphQL;

public class Mutation
{
    [Serial]
    [Authorize(Roles = [nameof(RoleNames.Moderator)])]
    public async Task<BoardGame?> UpdateBoardGame([Service] ApplicationDbContext context, BoardGameDTO model)
    {
        var boardgame = await context.BoardGames.FindAsync(model.Id);
        if (boardgame == null) return boardgame;

        if (!string.IsNullOrEmpty(model.Name))
            boardgame.Name = model.Name;
        if (model.Year.HasValue && model.Year.Value > 0)
            boardgame.Year = model.Year.Value;
        boardgame.LastModifiedDate = DateTime.Now;

        context.BoardGames.Update(boardgame);
        await context.SaveChangesAsync();
        return boardgame;
    }

    [Serial]
    [Authorize(Roles = [nameof(RoleNames.Administrator)])]
    public async Task DeleteBoardGame([Service] ApplicationDbContext context, int id)
    {
        var boardgame = await context.BoardGames.FindAsync(id);
        if (boardgame == null) return;

        context.BoardGames.Remove(boardgame);
        await context.SaveChangesAsync();
    }

    [Serial]
    [Authorize(Roles = [nameof(RoleNames.Moderator)])]
    public async Task<Domain?> UpdateDomain([Service] ApplicationDbContext context, DomainDTO model)
    {
        var domain = await context.Domains.FindAsync(model.Id);
        if (domain == null) return domain;

        if (!string.IsNullOrEmpty(model.Name)) domain.Name = model.Name;
        domain.LastModifiedDate = DateTime.Now;

        context.Domains.Update(domain);
        await context.SaveChangesAsync();
        return domain;
    }

    [Serial]
    [Authorize(Roles = [nameof(RoleNames.Administrator)])]
    public async Task DeleteDomain([Service] ApplicationDbContext context, int id)
    {
        var domain = await context.Domains.FindAsync(id);
        if (domain == null) return;

        context.Domains.Remove(domain);
        await context.SaveChangesAsync();
    }

    [Serial]
    [Authorize(Roles = [nameof(RoleNames.Moderator)])]
    public async Task<Mechanic?> UpdateMechanic([Service] ApplicationDbContext context, MechanicDTO model)
    {
        var mechanic = await context.Mechanics.FindAsync(model.Id);
        if (mechanic == null) return mechanic;

        if (!string.IsNullOrEmpty(model.Name)) mechanic.Name = model.Name;
        mechanic.LastModifiedDate = DateTime.Now;

        context.Mechanics.Update(mechanic);
        await context.SaveChangesAsync();
        return mechanic;
    }

    [Serial]
    [Authorize(Roles = [nameof(RoleNames.Administrator)])]
    public async Task DeleteMechanic([Service] ApplicationDbContext context, int id)
    {
        var mechanic = await context.Mechanics.FindAsync(id);
        if (mechanic == null) return;

        context.Mechanics.Remove(mechanic);
        await context.SaveChangesAsync();
    }
}