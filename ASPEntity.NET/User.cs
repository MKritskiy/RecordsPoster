using System.Text.Json.Serialization;

public class User
{
    public int Id { get; set; }
    public string? Nickname { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    [JsonIgnore]
    public List<Record> Records { get; set; } = new();
}
