namespace MyBGList.DTO.v1;

/// <summary>
/// Contains the data and the links that will be sent to the client
/// </summary>
public class RestDTO<T> {
    public T Data { get; set; } = default!;
    public List<LinkDTO> Links { get; set; } = new List<LinkDTO>();
}