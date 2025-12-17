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
  const [logoPreview, setLogoPreview] = useState('');
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [editingTenantId, setEditingTenantId] = useState<string | null>(null);
  const [isEditMode, setIsEditMode] = useState(false);
  const [isUploadingLogo, setIsUploadingLogo] = useState(false);

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
    
    if (name.startsWith('company.')) {
      const fieldName = name.replace('company.', '');
      setFormData(prev => ({
        ...prev,
        company: {
          ...prev.company,
          [fieldName]: fieldName === 'numberOfEmployees' ? parseInt(value) || 1 : value,
        },
      }));
    } else if (name.startsWith('tenantAdmin.')) {
      const fieldName = name.replace('tenantAdmin.', '');
      setFormData(prev => ({
        ...prev,
        tenantAdmin: {
          ...prev.tenantAdmin,
          [fieldName]: value,
        },
      }));
    } else {
      setFormData(prev => ({ ...prev, [name]: value }));
    }
  };

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const name = e.target.value;
    setFormData(prev => ({
      ...prev,
      name,
      slug: tenantService.generateSlug(name),
      company: {
        ...prev.company,
        name, // Auto-sync organization name with company name
      },
    }));
  };

  const handleTenantAdminChange = (field: string, value: string) => {
    setFormData(prev => {
      const updatedData = {
        ...prev,
        tenantAdmin: {
          ...prev.tenantAdmin,
          [field]: value,
        },
      };

      // Auto-sync tenant admin fields with contact person fields
      if (field === 'firstName' || field === 'lastName') {
        const fullName = `${updatedData.tenantAdmin.firstName} ${updatedData.tenantAdmin.lastName}`.trim();
        updatedData.company = {
          ...updatedData.company,
          contactPersonName: fullName,
        };
      } else if (field === 'email') {
        updatedData.company = {
          ...updatedData.company,
          contactPersonEmail: value,
        };
      } else if (field === 'phoneNumber') {
        updatedData.company = {
          ...updatedData.company,
          contactPersonPhone: value,
        };
      }

      return updatedData;
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!token) {
      toast.error('Authentication required');
      return;
    }

    // Check if logo is still uploading
    if (isUploadingLogo) {
      toast.error('Please wait for logo upload to complete');
      return;
    }

    // Check if logo file is selected but not uploaded
    if (logoFile && !formData.company.logoUrl) {
      toast.error('Please wait for logo upload to complete');
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
    setLogoPreview('');
    setLogoFile(null);
  };

  const handleLogoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate file type
    if (!file.type.startsWith('image/')) {
      toast.error('Please select an image file');
      return;
    }

    // Validate file size (max 5MB)
    if (file.size > 5 * 1024 * 1024) {
      toast.error('Image size must be less than 5MB');
      return;
    }

    try {
      setIsUploadingLogo(true);
      setLogoFile(file);

      // Create preview
      const reader = new FileReader();
      reader.onloadend = () => {
        setLogoPreview(reader.result as string);
      };
      reader.readAsDataURL(file);

      // Upload to Cloudinary
      const formDataUpload = new FormData();
      formDataUpload.append('images', file);

      const response = await fetch('/api/admin/cloudinary/upload/bulk?folder=company-logos', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
        body: formDataUpload,
      });

      if (!response.ok) {
        throw new Error('Failed to upload logo');
      }

      const result = await response.json();

      if (result.results && result.results.length > 0 && result.results[0].Success) {
        const logoUrl = result.results[0].Url;
        setFormData(prev => ({
          ...prev,
          company: {
            ...prev.company,
            logoUrl: logoUrl,
          },
        }));
        toast.success('Logo uploaded successfully');
      } else {
        throw new Error('Upload failed');
      }
    } catch (error) {
      console.error('Error uploading logo:', error);
      toast.error('Failed to upload logo');
      setLogoPreview('');
      setLogoFile(null);
    } finally {
      setIsUploadingLogo(false);
    }
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
        name: data.Name,
        slug: data.Slug,
        description: data.Description || '',
        company: {
          name: data.Company?.Name || '',
          numberOfEmployees: data.Company?.NumberOfEmployees || 0,
          location: data.Company?.Location || '',
          industry: data.Company?.Industry || '',
          contactPersonName: data.Company?.ContactPersonName || '',
          contactPersonEmail: data.Company?.ContactPersonEmail || '',
          contactPersonRole: data.Company?.ContactPersonRole || '',
          contactPersonPhone: data.Company?.ContactPersonPhone || '',
          logoUrl: data.Company?.LogoUrl || '',
        },
        tenantAdmin: {
          firstName: adminFirstName,
          lastName: adminLastName,
          email: adminEmail,
          phoneNumber: adminPhone,
          password: '', // Password is never returned from API for security
        },
      });

      setLogoPreview(data.Company?.LogoUrl || '');
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

  const handleUpdateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!token || !editingTenantId) return;

    // Check if logo is still uploading
    if (isUploadingLogo) {
      toast.error('Please wait for logo upload to complete');
      return;
    }

    // Check if logo file is selected but not uploaded
    if (logoFile && !formData.company.logoUrl) {
      toast.error('Please wait for logo upload to complete');
      return;
    }

    try {
      setIsLoading(true);
      setError('');

      const updateData = {
        Name: formData.name,
        Slug: formData.slug,
        Description: formData.description,
        Company: {
          Name: formData.company.name,
          NumberOfEmployees: formData.company.numberOfEmployees,
          Location: formData.company.location,
          Industry: formData.company.industry,
          ContactPersonName: formData.company.contactPersonName,
          ContactPersonEmail: formData.company.contactPersonEmail,
          ContactPersonRole: formData.company.contactPersonRole,
          ContactPersonPhone: formData.company.contactPersonPhone,
          LogoUrl: formData.company.logoUrl,
        },
        TenantAdmin: {
          FirstName: formData.tenantAdmin.firstName,
          LastName: formData.tenantAdmin.lastName,
          Email: formData.tenantAdmin.email,
          // Password and PhoneNumber are NOT sent during updates
        },
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
          <form onSubmit={isEditMode ? handleUpdateSubmit : handleSubmit} className={styles.form}>
            <div className={styles.formGrid}>
              {/* Tenant Information */}
              <div className={styles.formSection}>
                
                <div className={styles.fieldGroup}>
                  <label className={`${styles.label} ${styles.required}`}>
                    Organization Name
                  </label>
                  <input
                    type="text"
                    name="name"
                    value={formData.name}
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
                    name="slug"
                    value={formData.slug}
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
                    name="description"
                    value={formData.description}
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
                  <label className={`${styles.label} ${styles.required}`}>
                    Number of Employees
                  </label>
                  <input
                    type="number"
                    name="company.numberOfEmployees"
                    value={formData.company.numberOfEmployees}
                    onChange={handleInputChange}
                    required
                    min="1"
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
                    name="company.location"
                    value={formData.company.location}
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
                    name="company.industry"
                    value={formData.company.industry}
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

                <div className={styles.fieldGroup}>
                  <label className={styles.label}>
                    Company Logo
                  </label>
                  <input
                    type="file"
                    accept="image/*"
                    onChange={handleLogoUpload}
                    className={styles.input}
                    disabled={isUploadingLogo}
                  />
                  {isUploadingLogo && (
                    <p style={{ fontSize: '14px', color: '#7c3aed', marginTop: '8px' }}>
                      Uploading logo...
                    </p>
                  )}
                  {logoPreview && (
                    <div style={{ marginTop: '12px' }}>
                      <img
                        src={logoPreview}
                        alt="Logo preview"
                        style={{
                          maxWidth: '128px',
                          maxHeight: '128px',
                          objectFit: 'contain',
                          border: '1px solid #e5e7eb',
                          borderRadius: '4px',
                          padding: '8px'
                        }}
                      />
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Contact Person Information - Only unique fields, others auto-synced */}
            <div className={styles.formSection}>
              <h4 className={styles.sectionTitle}>Contact Person Information</h4>
              <div className={styles.formGrid}>
                {/* Contact Person Name, Email, Phone are auto-synced from Tenant Admin - hidden from UI */}

                <div className={styles.fieldGroup}>
                  <label className={`${styles.label} ${styles.required}`}>
                    Contact Person Role
                  </label>
                  <input
                    type="text"
                    name="company.contactPersonRole"
                    value={formData.company.contactPersonRole}
                    onChange={handleInputChange}
                    required
                    className={styles.input}
                    placeholder="e.g., HR Manager, CEO"
                  />
                </div>
              </div>
            </div>

            {/* Tenant Admin Information - Auto-syncs with Contact Person */}
            {!isEditMode && (
              <div className={styles.formSection}>
                <h4 className={styles.sectionTitle}>Tenant Admin Account</h4>
              <div className={styles.formGrid}>
                <div className={styles.fieldGroup}>
                  <label className={`${styles.label} ${styles.required}`}>
                    First Name
                  </label>
                  <input
                    type="text"
                    value={formData.tenantAdmin.firstName}
                    onChange={(e) => handleTenantAdminChange('firstName', e.target.value)}
                    required={!isEditMode}
                    className={styles.input}
                    placeholder="First name"
                  />
                </div>

                <div className={styles.fieldGroup}>
                  <label className={`${styles.label} ${styles.required}`}>
                    Last Name
                  </label>
                  <input
                    type="text"
                    value={formData.tenantAdmin.lastName}
                    onChange={(e) => handleTenantAdminChange('lastName', e.target.value)}
                    required={!isEditMode}
                    className={styles.input}
                    placeholder="Last name"
                  />
                </div>

                <div className={styles.fieldGroup}>
                  <label className={`${styles.label} ${styles.required}`}>
                    Email
                  </label>
                  <input
                    type="email"
                    value={formData.tenantAdmin.email}
                    onChange={(e) => handleTenantAdminChange('email', e.target.value)}
                    required={!isEditMode}
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
                        value={formData.tenantAdmin.phoneNumber}
                        onChange={(e) => handleTenantAdminChange('phoneNumber', e.target.value)}
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
                          value={formData.tenantAdmin.password}
                          onChange={(e) => handleTenantAdminChange('password', e.target.value)}
                          required
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

            <div className={styles.formActions}>
              <button
                type="button"
                onClick={handleCancelEdit}
                className={styles.cancelButton}
                disabled={isLoading}
              >
                Cancel
              </button>
              <button
                type="submit"
                className={styles.submitButton}
                disabled={isLoading}
              >
                {isLoading
                  ? (isEditMode ? 'Updating...' : 'Creating...')
                  : (isEditMode ? 'Update Company' : 'Create Company')
                }
              </button>
            </div>
          </form>
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
                      <span className={`${styles.statusBadge} ${
                        tenant.IsActive ? styles.statusActive : styles.statusInactive
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
