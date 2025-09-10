using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Nomad.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string Participant = "Participant";
    public const string TenantUser = "TenantUser";

    public static void AddPolicies(AuthorizationOptions options)
    {
        // SuperAdmin policy - can access everything
        options.AddPolicy(SuperAdmin, policy =>
            policy.RequireRole("SuperAdmin"));

        // TenantAdmin policy - can manage users and surveys within their tenant
        options.AddPolicy(TenantAdmin, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("SuperAdmin") ||
                context.User.IsInRole("TenantAdmin")));

        // Participant policy - can fill surveys assigned to them
        options.AddPolicy(Participant, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("SuperAdmin") ||
                context.User.IsInRole("TenantAdmin") ||
                context.User.IsInRole("Participant")));

        // TenantUser policy - any user belonging to a tenant
        options.AddPolicy(TenantUser, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("SuperAdmin") ||
                // context.User.HasClaim("TenantId", tenantId => !string.IsNullOrEmpty(tenantId))));
                context.User.HasClaim(c => c.Type == "TenantId" && !string.IsNullOrEmpty(c.Value))));

        // Permission-based policies
        options.AddPolicy("CanManageUsers", policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("SuperAdmin") ||
                context.User.IsInRole("TenantAdmin") ||
                context.User.HasClaim("Permission", "manage_users")));

        options.AddPolicy("CanManageSurveys", policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("SuperAdmin") ||
                context.User.IsInRole("TenantAdmin") ||
                context.User.HasClaim("Permission", "manage_surveys")));

        options.AddPolicy("CanViewReports", policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("SuperAdmin") ||
                context.User.IsInRole("TenantAdmin") ||
                context.User.HasClaim("Permission", "view_reports")));

        options.AddPolicy("CanFillSurveys", policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole("SuperAdmin") ||
                context.User.IsInRole("TenantAdmin") ||
                context.User.IsInRole("Participant") ||
                context.User.HasClaim("Permission", "fill_surveys")));
    }
}

public class AuthorizeTenantAttribute : AuthorizeAttribute
{
    public AuthorizeTenantAttribute(string? policy = null) : base(policy ?? AuthorizationPolicies.TenantUser)
    {
    }
}

public class AuthorizeTenantAdminAttribute : AuthorizeAttribute
{
    public AuthorizeTenantAdminAttribute() : base(AuthorizationPolicies.TenantAdmin)
    {
    }
}

public class AuthorizeSuperAdminAttribute : AuthorizeAttribute
{
    public AuthorizeSuperAdminAttribute() : base(AuthorizationPolicies.SuperAdmin)
    {
    }
}

public class AuthorizeParticipantAttribute : AuthorizeAttribute
{
    public AuthorizeParticipantAttribute() : base(AuthorizationPolicies.Participant)
    {
    }
}
