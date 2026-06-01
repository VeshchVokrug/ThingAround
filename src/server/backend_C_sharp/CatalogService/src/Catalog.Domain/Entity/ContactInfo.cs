namespace Domain.Entity;

public class ContactInfo
{
    public Guid ManagerId { get; set; }
    public string PersonName { get; set; }
    public string PersonPhone { get; set; }
    public List<string>? SocialsUrls { get; set; }
}