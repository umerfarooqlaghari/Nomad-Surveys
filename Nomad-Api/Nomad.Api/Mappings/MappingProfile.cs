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

        // Participant mappings
        CreateMap<Subject, SubjectDomain>()
            .ForMember(dest => dest.SubjectEvaluators, opt => opt.MapFrom(src => src.SubjectEvaluators));

        CreateMap<Evaluator, EvaluatorDomain>()
            .ForMember(dest => dest.SubjectEvaluators, opt => opt.MapFrom(src => src.SubjectEvaluators));

        CreateMap<SubjectEvaluator, SubjectEvaluatorDomain>();

        // Employee mappings
        CreateMap<Employee, EmployeeDomain>();
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

        // Participant domain to response mappings
        CreateMap<SubjectDomain, SubjectResponse>()
            .ForMember(dest => dest.AssignedEvaluatorIds, opt => opt.MapFrom(src => src.SubjectEvaluators.Where(se => se.IsActive).Select(se => se.EvaluatorId).ToList()));
        CreateMap<SubjectDomain, SubjectListResponse>()
            .ForMember(dest => dest.EvaluatorCount, opt => opt.MapFrom(src => src.SubjectEvaluators.Count(se => se.IsActive)));

        CreateMap<EvaluatorDomain, EvaluatorResponse>()
            .ForMember(dest => dest.AssignedSubjectIds, opt => opt.MapFrom(src => src.SubjectEvaluators.Where(se => se.IsActive).Select(se => se.SubjectId).ToList()));
        CreateMap<EvaluatorDomain, EvaluatorListResponse>()
            .ForMember(dest => dest.SubjectCount, opt => opt.MapFrom(src => src.SubjectEvaluators.Count(se => se.IsActive)));

        CreateMap<SubjectEvaluatorDomain, SubjectEvaluatorResponse>();

        // Employee domain to response mappings
        CreateMap<EmployeeDomain, EmployeeResponse>();
        CreateMap<EmployeeDomain, EmployeeListResponse>();
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

        // Participant request to domain mappings
        CreateMap<CreateSubjectRequest, SubjectDomain>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.SubjectEvaluators, opt => opt.Ignore());

        CreateMap<UpdateSubjectRequest, SubjectDomain>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.SubjectEvaluators, opt => opt.Ignore());

        CreateMap<CreateEvaluatorRequest, EvaluatorDomain>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.SubjectEvaluators, opt => opt.Ignore());

        CreateMap<UpdateEvaluatorRequest, EvaluatorDomain>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.SubjectEvaluators, opt => opt.Ignore());

        // Request to entity mappings for direct use
        CreateMap<CreateSubjectRequest, Subject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.SubjectEvaluators, opt => opt.Ignore());

        CreateMap<UpdateSubjectRequest, Subject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.SubjectEvaluators, opt => opt.Ignore());

        CreateMap<CreateEvaluatorRequest, Evaluator>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.SubjectEvaluators, opt => opt.Ignore());

        CreateMap<UpdateEvaluatorRequest, Evaluator>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.SubjectEvaluators, opt => opt.Ignore());

        // Employee request to entity mappings
        CreateMap<CreateEmployeeRequest, Employee>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore())
            .ForMember(dest => dest.Evaluator, opt => opt.Ignore());

        CreateMap<UpdateEmployeeRequest, Employee>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Subject, opt => opt.Ignore())
            .ForMember(dest => dest.Evaluator, opt => opt.Ignore());

        // UpdateEmployeeRequest to CreateEmployeeRequest for validation
        CreateMap<UpdateEmployeeRequest, CreateEmployeeRequest>();
    }

    private void CreateEntityToResponseMaps()
    {
        // Direct entity to response mappings for convenience
        CreateMap<Tenant, TenantResponse>()
            .ForMember(dest => dest.Company, opt => opt.MapFrom(src => src.Company));

        CreateMap<Tenant, TenantListResponse>()
            .ForMember(dest => dest.UserCount, opt => opt.MapFrom(src => src.Users.Count(u => u.IsActive)))
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company != null ? src.Company.Name : null));

        CreateMap<Company, CompanyResponse>()
            .ForMember(dest => dest.ContactPerson, opt => opt.MapFrom(src => src.ContactPerson != null ? new UserResponse
            {
                Id = src.ContactPerson.Id,
                UserName = src.ContactPerson.UserName ?? string.Empty,
                Email = src.ContactPerson.Email ?? string.Empty,
                FirstName = src.ContactPerson.FirstName ?? string.Empty,
                LastName = src.ContactPerson.LastName ?? string.Empty,
                FullName = (src.ContactPerson.FirstName ?? string.Empty) + " " + (src.ContactPerson.LastName ?? string.Empty),
                IsActive = src.ContactPerson.IsActive,
                EmailConfirmed = src.ContactPerson.EmailConfirmed,
                PhoneNumber = src.ContactPerson.PhoneNumber,
                CreatedAt = src.ContactPerson.CreatedAt,
                UpdatedAt = src.ContactPerson.UpdatedAt,
                LastLoginAt = src.ContactPerson.LastLoginAt,
                TenantId = src.ContactPerson.TenantId,
                Roles = new List<string>(),
                Permissions = new List<string>(),
                Tenant = null
            } : null));

        CreateMap<ApplicationUser, UserResponse>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore())
            .ForMember(dest => dest.Permissions, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore());

        CreateMap<ApplicationUser, UserListResponse>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore());

        CreateMap<TenantRole, RoleResponse>()
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.RolePermissions.Where(rp => rp.IsActive).Select(rp => rp.Permission)));

        CreateMap<Permission, PermissionResponse>();

        // Direct entity to response mappings for participants
        CreateMap<Subject, SubjectResponse>()
            .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant))
            .ForMember(dest => dest.Evaluators, opt => opt.Ignore()); // Handled in service

        CreateMap<Subject, SubjectListResponse>()
            .ForMember(dest => dest.EvaluatorCount, opt => opt.MapFrom(src => src.SubjectEvaluators.Count(se => se.IsActive)));

        CreateMap<Evaluator, EvaluatorResponse>()
            .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant))
            .ForMember(dest => dest.Subjects, opt => opt.Ignore()); // Handled in service

        CreateMap<Evaluator, EvaluatorListResponse>()
            .ForMember(dest => dest.SubjectCount, opt => opt.MapFrom(src => src.SubjectEvaluators.Count(se => se.IsActive)));

        CreateMap<SubjectEvaluator, SubjectEvaluatorResponse>()
            .ForMember(dest => dest.Subject, opt => opt.Ignore()) // Handled in service
            .ForMember(dest => dest.Evaluator, opt => opt.Ignore()); // Handled in service

        // Direct entity to response mappings for employees
        CreateMap<Employee, EmployeeResponse>();
        CreateMap<Employee, EmployeeListResponse>();
    }
}
