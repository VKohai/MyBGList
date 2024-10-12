namespace MyBGList.DTO.v2;

/// <summary>
/// Contains the data and the links that will be sent to the client
/// </summary>
public class RestDTO<T> {
    public T Items { get; set; } = default!;
    public List<MyBGList.DTO.v1.LinkDTO> Links { get; set; } = new List<MyBGList.DTO.v1.LinkDTO>();
}