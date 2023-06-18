namespace Cblx.Dynamics;

/// <summary>
/// This attibute enable the new mode for the Domain Entity (for Repositories) based on it's JSON template (ex: when using FlattenJsonConverter).
/// Thiw should be removed in the future, when the new SelectAndExpandParser and the new ChangeTracker are stable.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class UseNewJsonTemplateModeAttribute : Attribute { }
