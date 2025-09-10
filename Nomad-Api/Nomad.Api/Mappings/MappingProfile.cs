using AutoMapper;
using Nomad.Api.Domain.Models;
using Nomad.Api.DTOs.Request;
using Nomad.Api.DTOs.Response;
using Nomad.Api.Entities;

namespace Nomad.Api.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateEntityToDomainMaps();
        CreateDomainToResponseMaps();
        CreateRequestToDomainMaps();
        CreateEntityToResponseMaps();
    }

    private void CreateEntityToDomainMaps()
    {
        CreateMap<Tenant, TenantDomain>()
            .ForMember(dest => dest.Users, opt => opt.MapFrom(src => src.Users))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.TenantRoles))
            .ForMember(dest => dest.Company, opt => opt.MapFrom(src => src.Company));

        CreateMap<Company, CompanyDomain>();

        CreateMap<ApplicationUser, UserDomain>()
            .ForMember(dest => dest.UserRoles, opt => opt.MapFrom(src => src.UserTenantRoles))
            .ForMember(dest => dest.Roles, opt => opt.Ignore())
            .ForMember(dest => dest.Permissions, opt => opt.Ignore());

        CreateMap<TenantRole, RoleDomain>()
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.RolePermissions.Select(rp => rp.Permission)));

        CreateMap<Permission, PermissionDomain>();

        CreateMap<UserTenantRole, UserRoleDomain>();
    }

    private void CreateDomainToResponseMaps()
    {
        CreateMap<TenantDomain, TenantResponse>();
        CreateMap<TenantDomain, TenantListResponse>()
            .ForMember(dest => dest.UserCount, opt => opt.MapFrom(src => src.Users.Count(u => u.IsActive)))
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null));

        CreateMap<CompanyDomain, CompanyResponse>();

        CreateMap<UserDomain, UserResponse>();
        CreateMap<UserDomain, UserListResponse>();

        CreateMap<RoleDomain, RoleResponse>();

        CreateMap<PermissionDomain, PermissionResponse>();
    }

    private void CreateRequestToDomainMaps()
    {
        CreateMap<CreateTenantRequest, TenantDomain>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Users, opt => opt.Ignore())
            .ForMember(dest => dest.Roles, opt => opt.Ignore())
            .ForMember(dest => dest.Company, opt => opt.MapFrom(src => src.Company));

        CreateMap<CreateCompanyRequest, CompanyDomain>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.ContactPersonId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.ContactPerson, opt => opt.Ignore());

        CreateMap<CreateUserRequest, UserDomain>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles))
            .ForMember(dest => dest.Permissions, opt => opt.Ignore());
    }

    private void CreateEntityToResponseMaps()
    {
        // Direct entity to response mappings for convenience
        CreateMap<Tenant, TenantResponse>();
        CreateMap<Tenant, TenantListResponse>()
            .ForMember(dest => dest.UserCount, opt => opt.MapFrom(src => src.Users.Count(u => u.IsActive)))
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null));

        CreateMap<Company, CompanyResponse>();

        CreateMap<ApplicationUser, UserResponse>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore())
            .ForMember(dest => dest.Permissions, opt => opt.Ignore());

        CreateMap<ApplicationUser, UserListResponse>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore());

        CreateMap<TenantRole, RoleResponse>()
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.RolePermissions.Where(rp => rp.IsActive).Select(rp => rp.Permission)));

        CreateMap<Permission, PermissionResponse>();
    }
}
