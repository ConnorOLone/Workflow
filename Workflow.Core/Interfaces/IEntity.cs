public interface IEntity
{
    public string Name { get; set; }

    public EntityType EntityType {get; set;}
    
}
public enum EntityType
{
    UserType,
    GroupType,
    ServiceType,
    AdminType,
    SystemType
}