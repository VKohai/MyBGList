namespace MyBGList.GraphQL;

public class Query
{
    [Serial]
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<BoardGame> GetBoardGames(
        [Service] ApplicationDbContext dbContext) => dbContext.BoardGames;

    [Serial]
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Domain> GetDomains(
        [Service] ApplicationDbContext dbContext) => dbContext.Domains;

    [Serial]
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Mechanic> GetMechanics(
        [Service] ApplicationDbContext dbContext) => dbContext.Mechanics;
}