using System.Text.Json.Serialization;
using Homeless.Domain.Enums;
using Homeless.Domain.Events;
using Homeless.Domain.Exceptions;
using Homeless.Domain.ValueObjects;

namespace Homeless.Domain.Entities;

/// <summary>
/// Tenant aggregate root. Represents a single real estate agency (SaaS customer).
/// All tenant-scoped entities reference this via TenantId.
/// SuperAdmin controls lifecycle (Pending → Active → Suspended → Archived).
/// </summary>
public sealed class Tenant : Entity
{

    /// <summary>Human-readable unique slug used as subdomain (e.g. "century21-sp").</summary>
    [JsonInclude]
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Official registered name of the agency.</summary>
    [JsonInclude]
    public string Name { get; private set; } = string.Empty;

    /// <summary>Optional custom domain (e.g. "portal.century21sp.com.br").</summary>
    [JsonInclude]
    public string? CustomDomain { get; private set; }

    /// <summary>Tenant category (system tenant vs agency tenant).</summary>
    [JsonInclude]
    public TenantType Type { get; private set; } = TenantType.Agency;

    /// <summary>
    /// When non-empty, enables the tenant public portal <see cref="PortalLayoutMode"/> of <c>Custom</c>
    /// (bespoke UI in the platform Angular app, selected by tenant slug — see frontend custom manifests).
    /// If this value is an absolute URL, it is also used for OIDC redirect URI allowlisting and related
    /// origin checks — it does not by itself change Kubernetes Ingress routing (single shared frontend Service).
    /// When null or empty, the standard Default/Minimal/Premium portal layouts apply.
    /// </summary>
    [JsonInclude]
    public string? FrontendOrigin { get; private set; }

    [JsonInclude]
    public TenantStatus Status { get; private set; } = TenantStatus.Pending;

    [JsonInclude]
    public BrandingConfig Branding { get; private set; } = new();

    /// <summary>
    /// FK to the managed <see cref="Plan"/> entity. Null when the tenant has not yet
    /// been assigned to a structured plan (legacy / manually-configured tenants).
    /// </summary>
    [JsonInclude]
    public Guid? PlanId { get; private set; }

    [JsonInclude]
    public string? StripeCustomerId { get; private set; }
    [JsonInclude]
    public string? StripeSubscriptionId { get; private set; }
    [JsonInclude]
    public string PaymentStatus { get; private set; } = "none";
    [JsonInclude]
    public DateTime? LastPaymentDate { get; private set; }
    [JsonInclude]
    public DateTime? NextBillingDate { get; private set; }

    /// <summary>
    /// Derived frontend layout mode used by public portal route selection.
    /// - "Custom"  : <see cref="FrontendOrigin"/> is set; public portal uses the custom Angular sub-app.
    /// - "Premium" / "Minimal" / "Default" : from <c>portal_theme</c> entitlement (plan base).
    /// </summary>
    public string PortalLayoutMode
    {
        get
        {
            if (FrontendOrigin is { Length: > 0 })
                return "Custom";

            return ResolvePortalLayoutModeFromTheme();
        }
    }

    /// <summary>Raw plan entitlement: default, minimal, or premium.</summary>
    public string PortalTheme => ReadPortalThemeEntitlement();

    private string ReadPortalThemeEntitlement()
    {
        if (!Entitlements.TryGetValue("portal_theme", out var raw) || raw is null)
            return "default";

        var s = raw switch
        {
            string str => str,
            System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.String => je.GetString() ?? "",
            _ => raw.ToString() ?? ""
        };

        return s.Trim().ToLowerInvariant() switch
        {
            "minimal" => "minimal",
            "premium" => "premium",
            _ => "default"
        };
    }

    private string ResolvePortalLayoutModeFromTheme() =>
        ReadPortalThemeEntitlement() switch
        {
            "minimal" => "Minimal",
            "premium" => "Premium",
            _ => "Default"
        };

    /// <summary>
    /// Feature entitlements stored as JSONB.
    /// Keys: "max_properties" (int), "video_upload" (bool), "ai_descriptions" (bool),
    ///       "custom_domain" (bool), "premium_analytics" (bool), "portal_theme" (string: "default"|"minimal"|"premium").
    /// </summary>
    [JsonInclude]
    public Dictionary<string, object> Entitlements { get; private set; } = [];

    [JsonInclude]
    public string ContactEmail { get; private set; } = string.Empty;
    [JsonInclude]
    public string? ContactPhone { get; private set; }

    /// <summary>
    /// UserId of the person who signed up and created this tenant via self-service onboarding.
    /// Null for tenants created by SuperAdmin via the admin panel.
    /// </summary>
    [JsonInclude]
    public Guid? OwnerUserId { get; private set; }

    [JsonConstructor]
    private Tenant() { }

    public static Tenant Create(
        string name,
        string slug,
        string contactEmail,
        string? contactPhone = null,
        TenantType type = TenantType.Agency,
        string? frontendOrigin = null,
        Guid? ownerUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactEmail);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant().Trim(),
            ContactEmail = contactEmail.ToLowerInvariant().Trim(),
            ContactPhone = contactPhone,
            Type = type,
            FrontendOrigin = frontendOrigin?.Trim(),
            Branding = BrandingConfig.Default(name),
            Status = TenantStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Entitlements = DefaultEntitlements(),
            OwnerUserId = ownerUserId
        };

        return tenant;
    }

    public void SetOwnerUserId(Guid userId)
    {
        OwnerUserId = userId;
    }


    public void Activate()
    {
        if (Status == TenantStatus.Archived)
            throw new DomainException("TENANT_ARCHIVED", "Cannot activate an archived tenant.");

        Status = TenantStatus.Active;
        RaiseDomainEvent(new TenantActivatedEvent(Id, Slug));
    }

    public void Suspend(string reason)
    {
        if (Status != TenantStatus.Active)
            throw new DomainException("TENANT_NOT_ACTIVE", "Only active tenants can be suspended.");

        Status = TenantStatus.Suspended;
        RaiseDomainEvent(new TenantSuspendedEvent(Id, reason));
    }

    public void Archive()
    {
        Status = TenantStatus.Archived;
    }


    public void UpdateBranding(BrandingConfig branding)
    {
        ArgumentNullException.ThrowIfNull(branding);
        Branding = branding;
    }

    public void SetCustomDomain(string? domain)
    {
        if (!HasEntitlement("custom_domain"))
            throw new DomainException("FEATURE_NOT_ENTITLED", "Custom domain requires an eligible subscription plan.");

        CustomDomain = domain?.ToLowerInvariant().Trim();
    }

    public void SetFrontendOrigin(string? origin)
    {
        FrontendOrigin = origin?.Trim();
    }

    /// <summary>
    /// Updates basic tenant details. Used by SuperAdmin (all fields)
    /// and TenantAdmin (restricted subset enforced at application layer).
    /// </summary>
    public void UpdateDetails(
        string name,
        string slug,
        string contactEmail,
        string? contactPhone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactEmail);

        Name = name;
        Slug = slug.ToLowerInvariant().Trim();
        ContactEmail = contactEmail.ToLowerInvariant().Trim();
        ContactPhone = contactPhone;
    }

    /// <summary>
    /// Sets or clears the next billing date. Used by SuperAdmin when creating
    /// or editing a tenant to define the first payment cycle date.
    /// </summary>
    public void SetNextBillingDate(DateTime? nextBillingDate)
    {
        NextBillingDate = nextBillingDate;
    }

    public void SetType(TenantType type)
    {
        Type = type;
    }


    public void UpdateEntitlements(Dictionary<string, object> entitlements)
    {
        Entitlements = entitlements;
    }

    /// <summary>
    /// Grants a boolean feature flag. Called when an add-on is activated for this tenant.
    /// Idempotent: safe to call multiple times.
    /// </summary>
    public void GrantEntitlement(string featureKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureKey);
        Entitlements[featureKey] = true;
    }

    /// <summary>
    /// Revokes a boolean feature flag. Called when an add-on is cancelled or suspended.
    /// Does not remove the key — sets to false so the audit trail persists.
    /// </summary>
    public void RevokeEntitlement(string featureKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureKey);
        Entitlements[featureKey] = false;
    }

    /// <summary>
    /// Assigns the tenant to a managed <see cref="Plan"/> and merges the plan's base
    /// entitlements into the tenant's entitlements dictionary, preserving any active
    /// perk add-on keys (whatsapp_button, featured_listings, …) already set.
    /// </summary>
    public void ChangePlan(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        PlanId = plan.Id;

        foreach (var (key, value) in plan.ToBaseEntitlements())
            Entitlements[key] = value;
    }

    public void RecordPaymentSucceeded(DateTime paidAt, DateTime nextBilling)
    {
        PaymentStatus = "paid";
        LastPaymentDate = paidAt;
        NextBillingDate = nextBilling;

        if (Status == TenantStatus.Suspended)
            Activate();
    }

    public void RecordPaymentFailed()
    {
        PaymentStatus = "past_due";
    }

    public void SetStripeCustomerId(string customerId)
    {
        StripeCustomerId = customerId;
    }

    public void SetStripeSubscriptionId(string subscriptionId)
    {
        StripeSubscriptionId = subscriptionId;
    }

    public void SyncRecurringBilling(
        string? customerId,
        string? subscriptionId,
        string paymentStatus,
        DateTime? nextBillingDate)
    {
        if (!string.IsNullOrWhiteSpace(customerId))
            StripeCustomerId = customerId.Trim();

        StripeSubscriptionId = string.IsNullOrWhiteSpace(subscriptionId)
            ? null
            : subscriptionId.Trim();

        PaymentStatus = string.IsNullOrWhiteSpace(paymentStatus) ? PaymentStatus : paymentStatus.Trim();
        NextBillingDate = nextBillingDate;
    }

    public void MarkSubscriptionCanceled(DateTime? endsAt = null)
    {
        StripeSubscriptionId = null;
        PaymentStatus = "canceled";
        NextBillingDate = endsAt;
    }

    public bool IsPaymentOverdue(int graceDays = 7)
    {
        if (PaymentStatus != "past_due" || NextBillingDate is null)
            return false;

        return DateTime.UtcNow > NextBillingDate.Value.AddDays(graceDays);
    }

    public bool HasPendingPayment =>
        PaymentStatus is "past_due" or "incomplete" ||
        (PaymentStatus == "none" && PlanId is not null);

    public bool HasRecurringPayment =>
        !string.IsNullOrEmpty(StripeSubscriptionId);

    public bool HasEntitlement(string featureKey)
    {
        if (!Entitlements.TryGetValue(featureKey, out var value))
            return false;

        return value switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var parsed) && parsed,
            _ => false
        };
    }

    public T? GetEntitlement<T>(string featureKey)
    {
        if (!Entitlements.TryGetValue(featureKey, out var value))
            return default;

        try { return (T)Convert.ChangeType(value, typeof(T)); }
        catch { return default; }
    }

    private static Dictionary<string, object> DefaultEntitlements() => new()
    {
        ["max_properties"] = 10,
        ["video_upload"] = false,
        ["ai_descriptions"] = false,
        ["custom_domain"] = false,
        ["premium_analytics"] = false,
        ["portal_theme"] = "default"
    };
}
