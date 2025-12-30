/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import styles from './CompaniesTab.module.css';
import { tenantService, CreateTenantData, TenantListItem } from '@/services/tenantService';

export default function CompaniesTab() {
  const { token } = useAuth();
  const [showForm, setShowForm] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [tenants, setTenants] = useState<TenantListItem[]>([]);
  const [error, setError] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [formData, setFormData] = useState<CreateTenantData>(tenantService.getDefaultFormData());
  const [editingTenantId, setEditingTenantId] = useState<string | null>(null);
  const [isEditMode, setIsEditMode] = useState(false);

  // Load tenants on component mount
  useEffect(() => {
    loadTenants();
  }, [token]); // eslint-disable-line react-hooks/exhaustive-deps

  const loadTenants = async () => {
    if (!token) return;

    try {
      setIsLoading(true);
      setError('');

      const { data, error } = await tenantService.getTenants(token);

      if (error) {
        setError(error);
        console.error('Error loading tenants:', error);
      } else if (data) {
        setTenants(data);
      }
    } catch (err) {
      const errorMessage = 'Failed to load companies';
      setError(errorMessage);
      console.error('Error loading tenants:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;

    if (name.startsWith('Company.')) {
      const fieldName = name.replace('Company.', '');
      setFormData(prev => ({
        ...prev,
        Company: {
          ...prev.Company,
          [fieldName]: fieldName === 'NumberOfEmployees' ? (value ? parseInt(value) : null) : value,
        },
      }));
    } else if (name.startsWith('TenantAdmin.')) {
      const fieldName = name.replace('TenantAdmin.', '');
      setFormData(prev => ({
        ...prev,
        TenantAdmin: prev.TenantAdmin ? {
          ...prev.TenantAdmin,
          [fieldName]: value,
        } : { [fieldName]: value } as any,
      }));
    } else {
      setFormData(prev => ({ ...prev, [name]: value }));
    }
  };

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const name = e.target.value;
    setFormData(prev => ({
      ...prev,
      Name: name,
      Slug: tenantService.generateSlug(name),
      Company: {
        ...prev.Company,
        Name: name, // Auto-sync organization name with company name
      },
    }));
  };

  const handleTenantAdminChange = (field: string, value: string) => {
    setFormData(prev => {
      const updatedData = {
        ...prev,
        TenantAdmin: prev.TenantAdmin ? {
          ...prev.TenantAdmin,
          [field]: value,
        } : { [field]: value } as any,
      };

      // Auto-sync tenant admin fields with contact person fields if TenantAdmin exists
      if (updatedData.TenantAdmin) {
        if (field === 'FirstName' || field === 'LastName') {
          const fullName = `${updatedData.TenantAdmin.FirstName || ''} ${updatedData.TenantAdmin.LastName || ''}`.trim();
          updatedData.Company = {
            ...updatedData.Company,
            ContactPersonName: fullName,
          };
        } else if (field === 'Email') {
          updatedData.Company = {
            ...updatedData.Company,
            ContactPersonEmail: value,
          };
        } else if (field === 'PhoneNumber') {
          updatedData.Company = {
            ...updatedData.Company,
            ContactPersonPhone: value,
          };
        }
      }

      return updatedData;
    });
  };

  const handleSubmit = async (e?: React.FormEvent | React.MouseEvent) => {
    if (e) e.preventDefault();

    if (!token) {
      toast.error('Authentication required');
      return;
    }

    // Validate form data
    const validation = tenantService.validateTenantData(formData);
    if (!validation.isValid) {
      setError(validation.errors.join(', '));
      toast.error('Please fix the form errors');
      return;
    }

    setError('');
    setIsLoading(true);

    try {
      const { data, error } = await tenantService.createTenant(formData, token);

      if (error) {
        setError(error);
        toast.error(error);
      } else if (data) {
        toast.success('Company created successfully!');
        setShowForm(false);
        resetForm();
        await loadTenants(); // Reload the list
      }
    } catch (err: any) {
      const errorMessage = err.message || 'Failed to create company';
      setError(errorMessage);
      toast.error(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const resetForm = () => {
    setFormData(tenantService.getDefaultFormData());
  };

  const handleEditClick = async (tenantId: string) => {
    if (!token) return;

    try {
      setIsLoading(true);
      const { data, error } = await tenantService.getTenantById(tenantId, token);

      if (error || !data) {
        toast.error(error || 'Failed to load tenant details');
        return;
      }

      // Convert TenantResponse to CreateTenantData format for editing
      // Populate admin details from TenantAdmin (retrieved via Roles → UserTenantRoles → Users)
      // TenantAdmin is populated by querying Users table through UserTenantRoles relationship
      const adminFirstName = data.TenantAdmin?.FirstName || '';
      const adminLastName = data.TenantAdmin?.LastName || '';
      const adminEmail = data.TenantAdmin?.Email || '';
      const adminPhone = data.TenantAdmin?.PhoneNumber || '';

      setFormData({
        Name: data.Name,
        Slug: data.Slug,
        Description: data.Description || '',
        Company: {
          Name: data.Company?.Name || '',
          NumberOfEmployees: data.Company?.NumberOfEmployees ?? null,
          Location: data.Company?.Location || '',
          Industry: data.Company?.Industry || '',
          ContactPersonName: data.Company?.ContactPersonName || '',
          ContactPersonEmail: data.Company?.ContactPersonEmail || '',
          ContactPersonRole: data.Company?.ContactPersonRole || null,
          ContactPersonPhone: data.Company?.ContactPersonPhone || null,
          LogoUrl: data.Company?.LogoUrl || null,
        },
        TenantAdmin: data.TenantAdmin ? {
          FirstName: data.TenantAdmin.FirstName || '',
          LastName: data.TenantAdmin.LastName || '',
          Email: data.TenantAdmin.Email || '',
          PhoneNumber: data.TenantAdmin.PhoneNumber || '',
          Password: '',
        } : null,
      });

      setEditingTenantId(tenantId);
      setIsEditMode(true);
      setShowForm(true);
    } catch (err) {
      toast.error('Failed to load tenant details');
      console.error('Error loading tenant:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancelEdit = () => {
    setIsEditMode(false);
    setEditingTenantId(null);
    setShowForm(false);
    resetForm();
  };

  const handleUpdateSubmit = async (e?: React.FormEvent | React.MouseEvent) => {
    if (e) e.preventDefault();

    if (!token || !editingTenantId) return;

    try {
      setIsLoading(true);
      setError('');

      const updateData = {
        Name: formData.Name,
        Slug: formData.Slug,
        Description: formData.Description,
        Company: {
          Name: formData.Company.Name,
          NumberOfEmployees: formData.Company.NumberOfEmployees,
          Location: formData.Company.Location,
          Industry: formData.Company.Industry,
          ContactPersonName: formData.Company.ContactPersonName,
          ContactPersonEmail: formData.Company.ContactPersonEmail,
          ContactPersonRole: formData.Company.ContactPersonRole,
          ContactPersonPhone: formData.Company.ContactPersonPhone,
          LogoUrl: formData.Company.LogoUrl,
        },
        // TenantAdmin is not sent during updates since admin details are not editable in the UI
        TenantAdmin: null,
      };

      const { error } = await tenantService.updateTenant(editingTenantId, updateData, token);

      if (error) {
        setError(error);
        toast.error(error);
        return;
      }

      toast.success('Company updated successfully!');
      handleCancelEdit();
      await loadTenants();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update company';
      setError(errorMessage);
      toast.error(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h2>Companies</h2>
          <p>Manage organization onboarding and information (Updated)</p>
        </div>
        <button
          onClick={() => {
            if (showForm) {
              handleCancelEdit();
            } else {
              setShowForm(true);
            }
          }}
          className={styles.addButton}
          disabled={isLoading}
        >
          {showForm ? 'Cancel' : 'Add Company'}
        </button>
      </div>

      {/* Error Message */}
      {error && (
        <div className={styles.errorMessage}>
          {error}
        </div>
      )}

      {/* Onboarding Form */}
      {showForm && (
        <div className={styles.formCard}>
          <h3 className={styles.formTitle}>
            {isEditMode ? 'Edit Company' : 'Organization Onboarding'}
          </h3>
          <form className={styles.form}>
            <div className={styles.formGrid}>
              {/* Tenant Information */}
              <div className={styles.formSection}>

                <div className={styles.fieldGroup}>
                  <label className={`${styles.label} ${styles.required}`}>
                    Organization Name
                  </label>
                  <input
                    type="text"
                    name="Name"
                    value={formData.Name}
                    onChange={handleNameChange}
                    required
                    className={styles.input}
                    placeholder="Enter organization name"
                  />
                </div>

                <div className={styles.fieldGroup}>
                  <label className={`${styles.label} ${styles.required}`}>
                    Tenant Slug
                  </label>
                  <input
                    type="text"
                    name="Slug"
                    value={formData.Slug}
                    onChange={handleInputChange}
                    required
                    className={styles.input}
                    placeholder="organization-slug"
                  />
                </div>

                <div className={styles.fieldGroup}>
                  <label className={styles.label}>
                    Description
                  </label>
                  <textarea
                    name="Description"
                    value={formData.Description}
                    onChange={handleInputChange}
                    className={styles.textarea}
                    placeholder="Brief description of the organization"
                  />
                </div>
              </div>

              {/* Company Information */}
              <div className={styles.formSection}>

                {/* Company Name is auto-synced with Organization Name - hidden from UI */}

                <div className={styles.fieldGroup}>
                  <label className={styles.label}>
                    Number of Employees
                  </label>
                  <input
                    type="number"
                    name="Company.NumberOfEmployees"
                    value={formData.Company.NumberOfEmployees ?? ''}
                    onChange={handleInputChange}
                    className={styles.input}
                    placeholder="Number of employees"
                  />
                </div>

                <div className={styles.fieldGroup}>
                  <label className={`${styles.label} ${styles.required}`}>
                    Location
                  </label>
                  <input
                    type="text"
                    name="Company.Location"
                    value={formData.Company.Location}
                    onChange={handleInputChange}
                    required
                    className={styles.input}
                    placeholder="Company location"
                  />
                </div>

                <div className={styles.fieldGroup}>
                  <label className={`${styles.label} ${styles.required}`}>
                    Industry
                  </label>
                  <select
                    name="Company.Industry"
                    value={formData.Company.Industry}
                    onChange={handleInputChange}
                    required
                    className={styles.select}
                  >
                    <option value="">Select Industry</option>
                    <option value="Technology">Technology</option>
                    <option value="Healthcare">Healthcare</option>
                    <option value="Finance">Finance</option>
                    <option value="Education">Education</option>
                    <option value="Manufacturing">Manufacturing</option>
                    <option value="Retail">Retail</option>
                    <option value="Consulting">Consulting</option>
                    <option value="Other">Other</option>
                  </select>
                </div>


              </div>
            </div>

            {/* Contact Person Information - Display in edit mode, auto-synced in create mode */}
            <div className={styles.formSection}>
              <h4 className={styles.sectionTitle}>Contact Person Information</h4>
              <div className={styles.formGrid}>
                {isEditMode ? (
                  <>
                    {/* In edit mode, only show contact person phone and role */}
                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        Contact Person Phone
                      </label>
                      <input
                        type="tel"
                        value={formData.Company.ContactPersonPhone || 'Not provided'}
                        readOnly
                        className={`${styles.input} ${styles.readOnly}`}
                        style={{ backgroundColor: '#f3f4f6', cursor: 'not-allowed' }}
                      />
                    </div>

                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        Contact Person Role
                      </label>
                      <input
                        type="text"
                        name="Company.ContactPersonRole"
                        value={formData.Company.ContactPersonRole ?? ''}
                        onChange={handleInputChange}
                        className={styles.input}
                        placeholder="e.g., HR Manager, CEO"
                      />
                    </div>
                  </>
                ) : (
                  <>
                    {/* In create mode, only show role - name, email, phone are auto-synced from Tenant Admin */}
                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        Contact Person Role
                      </label>
                      <input
                        type="text"
                        name="Company.ContactPersonRole"
                        value={formData.Company.ContactPersonRole ?? ''}
                        onChange={handleInputChange}
                        className={styles.input}
                        placeholder="e.g., HR Manager, CEO"
                      />
                    </div>
                  </>
                )}
              </div>
            </div>

            {/* Tenant Admin Information - Auto-syncs with Contact Person */}
            {!isEditMode && (
              <div className={styles.formSection}>
                <h4 className={styles.sectionTitle}>Tenant Admin Account</h4>
                <div className={styles.formGrid}>
                  <div className={styles.fieldGroup}>
                    <label className={styles.label}>
                      First Name
                    </label>
                    <input
                      type="text"
                      value={formData.TenantAdmin?.FirstName ?? ''}
                      onChange={(e) => handleTenantAdminChange('FirstName', e.target.value)}
                      className={styles.input}
                      placeholder="First name"
                    />
                  </div>

                  <div className={styles.fieldGroup}>
                    <label className={styles.label}>
                      Last Name
                    </label>
                    <input
                      type="text"
                      value={formData.TenantAdmin?.LastName ?? ''}
                      onChange={(e) => handleTenantAdminChange('LastName', e.target.value)}
                      className={styles.input}
                      placeholder="Last name"
                    />
                  </div>

                  <div className={styles.fieldGroup}>
                    <label className={styles.label}>
                      Email
                    </label>
                    <input
                      type="email"
                      value={formData.TenantAdmin?.Email ?? ''}
                      onChange={(e) => handleTenantAdminChange('Email', e.target.value)}
                      className={styles.input}
                      placeholder="admin@company.com"
                    />
                  </div>

                  {/* Phone Number and Password - Only show in create mode */}
                  {!isEditMode && (
                    <>
                      <div className={styles.fieldGroup}>
                        <label className={styles.label}>
                          Phone Number
                        </label>
                        <input
                          type="tel"
                          value={formData.TenantAdmin?.PhoneNumber ?? ''}
                          onChange={(e) => handleTenantAdminChange('PhoneNumber', e.target.value)}
                          className={styles.input}
                          placeholder="+1 (555) 123-4567"
                        />
                      </div>

                      <div className={styles.fieldGroup}>
                        <label className={`${styles.label} ${styles.required}`}>
                          Password
                        </label>
                        <div className={styles.passwordContainer}>
                          <input
                            type={showPassword ? "text" : "password"}
                            value={formData.TenantAdmin?.Password ?? ''}
                            onChange={(e) => handleTenantAdminChange('Password', e.target.value)}
                            minLength={6}
                            className={styles.input}
                            placeholder="Minimum 6 characters"
                          />
                          <button
                            type="button"
                            onClick={() => setShowPassword(!showPassword)}
                            className={styles.passwordToggle}
                          >
                            {showPassword ? 'HIDE' : 'SHOW'}
                          </button>
                        </div>
                      </div>
                    </>
                  )}
                </div>
              </div>
            )}
          </form>

          {/* Action buttons outside form */}
          <div className={styles.formActions}>
            <button
              type="button"
              onClick={isEditMode ? handleUpdateSubmit : handleSubmit}
              className={styles.submitButton}
              disabled={isLoading}
            >
              {isLoading
                ? (isEditMode ? 'Updating...' : 'Creating...')
                : (isEditMode ? 'Update Company' : 'Create Company')
              }
            </button>
            <button
              type="button"
              onClick={handleCancelEdit}
              className={styles.cancelButton}
              disabled={isLoading}
            >
              Cancel
            </button>
          </div>
        </div>
      )}

      {/* Companies List */}
      <div className={styles.companiesCard}>
        <div className={styles.companiesHeader}>
          <h3 className={styles.companiesTitle}>Companies List</h3>
        </div>
        <div className={styles.tableContainer}>
          <table className={styles.table}>
            <thead className={styles.tableHead}>
              <tr>
                <th className={styles.tableHeader}>Company</th>
                <th className={styles.tableHeader}>Tenant Slug</th>
                <th className={styles.tableHeader}>Users</th>
                <th className={styles.tableHeader}>Status</th>
                <th className={styles.tableHeader}>Created</th>
                <th className={styles.tableHeader}>Actions</th>
              </tr>
            </thead>
            <tbody className={styles.tableBody}>
              {isLoading ? (
                <tr>
                  <td colSpan={6} className={styles.tableCell}>
                    Loading companies...
                  </td>
                </tr>
              ) : tenants.length === 0 ? (
                <tr>
                  <td colSpan={6} className={styles.tableCell}>
                    No companies found. Create your first company above.
                  </td>
                </tr>
              ) : (
                tenants.map((tenant) => (
                  <tr key={tenant.Id} className={styles.tableRow}>
                    <td className={styles.tableCell}>
                      <div className={styles.companyInfo}>
                        <div className={styles.logoContainer}>
                          {tenant.LogoUrl ? (
                            <img
                              src={tenant.LogoUrl}
                              alt={`${tenant.CompanyName || tenant.Name} logo`}
                              className={styles.logoImage}
                              onError={(e) => {
                                e.currentTarget.style.display = 'none';
                                const placeholder = e.currentTarget.nextElementSibling as HTMLElement;
                                if (placeholder) placeholder.style.display = 'flex';
                              }}
                            />
                          ) : null}
                          <div
                            className={styles.logoPlaceholder}
                            style={{ display: tenant.LogoUrl ? 'none' : 'flex' }}
                          >
                            {tenant.CompanyName?.[0] || tenant.Name?.[0] || 'C'}
                          </div>
                        </div>
                        <div className={styles.companyDetails}>
                          <div className={styles.companyName}>{tenant.CompanyName || tenant.Name}</div>
                          <div className={styles.companySlug}>{tenant.Name}</div>
                        </div>
                      </div>
                    </td>
                    <td className={styles.tableCell}>{tenant.Slug}</td>
                    <td className={styles.tableCell}>{tenant.UserCount}</td>
                    <td className={styles.tableCell}>
                      <span className={`${styles.statusBadge} ${tenant.IsActive ? styles.statusActive : styles.statusInactive
                        }`}>
                        {tenant.IsActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className={styles.tableCell}>
                      {new Date(tenant.CreatedAt).toLocaleDateString()}
                    </td>
                    <td className={styles.tableCell}>
                      <div className={styles.actionButtons}>
                        <button className={`${styles.actionButton} ${styles.viewButton}`}>
                          View
                        </button>
                        <button
                          className={`${styles.actionButton} ${styles.editButton}`}
                          onClick={() => handleEditClick(tenant.Id)}
                          disabled={isLoading}
                        >
                          Edit
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

    </div>
  );
}
