using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Orchestrator.Domain;

/// <summary>
/// Base interface for all domain entities
/// </summary>
public interface IEntity
{
    Guid Id { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Base class for all domain entities
/// </summary>
public abstract class Entity : IEntity
{
    /// <summary>
    /// The unique identifier of the entity
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// When this entity was created
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this entity was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// The user who created this entity
    /// </summary>
    [StringLength(255)]
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// The user who last updated this entity
    /// </summary>
    [StringLength(100)]
    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Whether this entity has been soft deleted
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When this entity was soft deleted
    /// </summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// The user who soft deleted this entity
    /// </summary>
    [StringLength(100)]
    [Column("deleted_by")]
    public string? DeletedBy { get; set; }
}