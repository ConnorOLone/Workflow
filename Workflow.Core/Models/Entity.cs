public abstract class Entity : IEntity
{
    public abstract string Name { get; set; }
    public abstract EntityType EntityType { get; set; }
}