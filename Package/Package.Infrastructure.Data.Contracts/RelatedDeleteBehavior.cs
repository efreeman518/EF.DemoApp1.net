namespace Package.Infrastructure.Data.Contracts;

/// <summary>
/// Indicates how to handle related entity deletions.
/// </summary>
public enum RelatedDeleteBehavior
{
    None = 0, // Do not remove any related entities
    RelationshipOnly = 1, // Remove immediate relationship only, not any m-m related entities themselves
    RelationshipAndEntity = 2 // Remove both immediate relationship and the m-m related entities themselves
}
