/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import styles from './CompaniesTab.module.css';
import { tenantService, CreateTenantData, TenantListItem } from '@/services/tenantService';
import DeleteConfirmationModal from '@/components/modals/DeleteConfirmationModal';

export default function ProjectsTab() {
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
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [tenantToDelete, setTenantToDelete] = useState<TenantListItem | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Load tenants on component mount
  useEffect(() => {
    loadTenants();
  }, [token]); // eslint-disable-line react-hooks/exhaustive-deps

  const loadTenants = async () => {
    if (!token) return;

    setIsLoading(true);
    try {
      const response = await tenantService.getTenants(token);
      if (response.error) {
        setTenants([]);
        setError(response.error);
        toast.error(response.error);
      } else {
        setTenants(response.data || []);
        setError('');
      }
    } catch (err: any) {
      console.error('Error loading tenants:', err);
      setError(err.message || 'Failed to load companies');
      toast.error('Failed to load companies');
    } finally {
      setIsLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;

    // Handle nested object updates
    if (name.startsWith('Company.')) {
      const fieldName = name.replace('Company.', '');
      setFormData(prev => ({
        ...prev,
        Company: {
          ...prev.Company,
          [fieldName]: fieldName === 'NumberOfEmployees' ? (parseInt(value) || 0) : value
        }
      }));
    } else if (name.startsWith('TenantAdmin.')) {
      const fieldName = name.replace('TenantAdmin.', '');
      setFormData(prev => ({
        ...prev,
        TenantAdmin: { ...prev.TenantAdmin, [fieldName]: value }
      }));
    } else {
      setFormData(prev => ({
        ...prev,
        [name]: value
      }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

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

    // Check if logo is still uploading
    if (isUploadingLogo) {
      toast.error('Please wait for logo upload to complete');
      return;
    }

    // Check if logo file is selected but not uploaded (safety check)
    if (logoFile && !formData.Company.LogoUrl) {
      toast.error('Logo is still processing. Please wait.');
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      const response = await tenantService.createTenant(formData, token);

      if (response.error) {
        setError(response.error);
        toast.error(response.error);
        return;
      }

      toast.success('Company created successfully!');
      setShowForm(false);
      setFormData(tenantService.getDefaultFormData());
      setLogoPreview('');
      setLogoFile(null);
      await loadTenants(); // Reload the list
    } catch (err: any) {
      const errorMessage = err.message || 'Failed to create Company';
      setError(errorMessage);
      toast.error(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const handleViewProject = (tenant: TenantListItem) => {
    // Open project dashboard in new tab
    const projectUrl = `/projects/${tenant.Slug}/dashboard`;
    window.open(projectUrl, '_blank');
  };

  const handleLogoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) {
      return;
    }

    // Validate file type
    if (!file.type.startsWith('image/')) {
      toast.error('Please select an image file');
      e.target.value = ''; // Reset input
      return;
    }

    // Validate file size (max 5MB)
    if (file.size > 5 * 1024 * 1024) {
      toast.error('Image size must be less than 5MB');
      e.target.value = ''; // Reset input
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
      reader.onerror = () => {
        toast.error('Failed to read image file');
        setIsUploadingLogo(false);
        e.target.value = '';
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
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error || 'Failed to upload logo');
      }

      const result = await response.json();

      if (result.results && result.results.length > 0 && result.results[0].Success) {
        const logoUrl = result.results[0].SecureUrl;
        setFormData(prev => ({
          ...prev,
          Company: {
            ...prev.Company,
            LogoUrl: logoUrl,
          },
        }));
        toast.success('Logo uploaded successfully');
        e.target.value = ''; // Reset input to allow re-uploading
      } else {
        const errorMsg = result.results?.[0]?.ErrorMessage || 'Upload failed - invalid response';
        throw new Error(errorMsg);
      }
    } catch (error: any) {
      const errorMessage = error.message || 'Failed to upload logo';
      toast.error(errorMessage);
      setLogoPreview('');
      setLogoFile(null);
      e.target.value = ''; // Reset input on error
    } finally {
      setIsUploadingLogo(false);
    }
  };

  const handleRemoveLogo = () => {
    setLogoFile(null);
    setLogoPreview('');
    setFormData(prev => ({
      ...prev,
      Company: {
        ...prev.Company,
        LogoUrl: ''
      }
    }));
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleDeleteClick = (tenant: TenantListItem) => {
    setTenantToDelete(tenant);
    setShowDeleteModal(true);
  };

  const handleConfirmDelete = async () => {
    if (!tenantToDelete || !token) return;

    setIsLoading(true);
    try {
      const response = await tenantService.deactivateTenant(tenantToDelete.Id, token);
      if (response.error) {
        toast.error(response.error);
      } else {
        toast.success(`Company ${tenantToDelete.CompanyName || tenantToDelete.Name} deactivated successfully`);
        await loadTenants();
      }
    } catch (err: any) {
      toast.error('Failed to deactivate company');
    } finally {
      setIsLoading(false);
      setShowDeleteModal(false);
      setTenantToDelete(null);
    }
  };

  const handleCancelDelete = () => {
    setShowDeleteModal(false);
    setTenantToDelete(null);
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
      setFormData({
        Name: data.Name,
        Slug: data.Slug,
        Description: data.Description || '',
        Company: {
          Name: data.Company?.Name || '',
          NumberOfEmployees: data.Company?.NumberOfEmployees || 0,
          Location: data.Company?.Location || '',
          Industry: data.Company?.Industry || '',
          ContactPersonName: data.Company?.ContactPersonName || '',
          ContactPersonEmail: data.Company?.ContactPersonEmail || '',
          ContactPersonRole: data.Company?.ContactPersonRole || '',
          ContactPersonPhone: data.Company?.ContactPersonPhone || '',
          LogoUrl: data.Company?.LogoUrl || '',
        },
        TenantAdmin: {
          FirstName: data.TenantAdmin?.FirstName || '',
          LastName: data.TenantAdmin?.LastName || '',
          Email: data.TenantAdmin?.Email || '',
          PhoneNumber: data.TenantAdmin?.PhoneNumber || '',
          Password: '', // Password is never returned from API for security
        },
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
    setFormData(tenantService.getDefaultFormData());
    setLogoPreview('');
    setLogoFile(null);
  };

  const handleUpdateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!token || !editingTenantId) {
      toast.error('Authentication required');
      return;
    }

    // Since validation is partially based on things like admin password (which we don't have on edit),
    // we should be careful here. However, tenantService.validateTenantData might need 
    // a variant for updates or we can just validate the company fields locally.

    // For simplicity, we'll validate the basic requirements
    if (!formData.Name.trim() || !formData.Slug.trim()) {
      toast.error('Name and slug are required');
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      const updateData = {
        ...formData,
        TenantAdmin: null, // Ensure TenantAdmin is null for updates
      };

      const response = await tenantService.updateTenant(editingTenantId, updateData, token);

      if (response.error) {
        setError(response.error);
        toast.error(response.error);
        return;
      }

      toast.success('Company updated successfully!');
      handleCancelEdit();
      await loadTenants();
    } catch (err: any) {
      const errorMessage = err.message || 'Failed to update company';
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
          <p>Manage company onboarding and information</p>
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
          disabled={isLoading || isUploadingLogo}
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

      {/* Add Project Form */}
      {showForm && (
        <div className={styles.formCard}>
          <div className={styles.formHeader}>
            <h3 className={styles.formTitle}>
              {isEditMode ? 'Edit Company' : 'Add New Company'}
            </h3>
          </div>
          <form onSubmit={isEditMode ? handleUpdateSubmit : handleSubmit} className={styles.form}>
            <div className={styles.formGrid}>
              {/* Project Name */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Project Name <span className={styles.required}></span>
                </label>
                <input
                  type="text"
                  name="Name"
                  value={formData.Name}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter project name"
                  required
                />
              </div>

              {/* Company Name */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Company Name <span className={styles.required}></span>
                </label>
                <input
                  type="text"
                  name="Company.Name"
                  value={formData.Company.Name}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter company name"
                  required
                />
              </div>

              {/* Slug */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Company Slug <span className={styles.required}></span>
                </label>
                <input
                  type="text"
                  name="Slug"
                  value={formData.Slug}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter company slug (e.g., company-name)"
                  required
                />
              </div>

              {/* Description */}
              <div className={styles.formGroup}>
                <label className={styles.label}>Description</label>
                <textarea
                  name="Description"
                  value={formData.Description}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter company description"
                  rows={3}
                />
              </div>

              {/* Number of Employees */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Number of Employees <span className={styles.required}></span>
                </label>
                <input
                  type="number"
                  name="Company.NumberOfEmployees"
                  value={formData.Company.NumberOfEmployees}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter number of employees"
                  required
                  min="1"
                />
              </div>

              {/* Industry */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Industry <span className={styles.required}></span>
                </label>
                <input
                  type="text"
                  name="Company.Industry"
                  value={formData.Company.Industry}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter industry"
                  required
                />
              </div>

              {/* Location */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Location <span className={styles.required}></span>
                </label>
                <input
                  type="text"
                  name="Company.Location"
                  value={formData.Company.Location}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter location"
                  required
                />
              </div>

              {/* Contact Person Name */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Contact Person Name <span className={styles.required}></span>
                </label>
                <input
                  type="text"
                  name="Company.ContactPersonName"
                  value={formData.Company.ContactPersonName}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter contact person name"
                  required
                />
              </div>

              {/* Contact Person Email */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Contact Person Email <span className={styles.required}></span>
                </label>
                <input
                  type="email"
                  name="Company.ContactPersonEmail"
                  value={formData.Company.ContactPersonEmail}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter contact person email"
                  required
                />
              </div>

              {/* Contact Person Role */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Contact Person Role <span className={styles.required}></span>
                </label>
                <input
                  type="text"
                  name="Company.ContactPersonRole"
                  value={formData.Company.ContactPersonRole}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter contact person role"
                  required
                />
              </div>

              {/* Contact Person Phone */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Contact Person Phone <span className={styles.required}></span>
                </label>
                <input
                  type="tel"
                  name="Company.ContactPersonPhone"
                  value={formData.Company.ContactPersonPhone}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter contact person phone (e.g., +1234567890)"
                  required
                />
              </div>

              {/* Company Logo - Only show in create mode */}
               
                <div className={styles.formGroup}>
                  <label className={styles.label}>Company Logo</label>
                  <input
                    type="file"
                    ref={fileInputRef}
                    accept="image/*"
                    onChange={handleLogoUpload}
                    className={styles.input}
                    disabled={isUploadingLogo || isLoading}
                  />
                  {isUploadingLogo && (
                    <p style={{ marginTop: '8px', color: '#7c3aed' }}>Uploading logo...</p>
                  )}
                  {logoPreview && (
                    <div className={styles.logoPreviewWrapper}>
                      <img
                        src={logoPreview}
                        alt="Logo preview"
                        className={styles.logoPreviewImg}
                      />
                      <button
                        type="button"
                        onClick={handleRemoveLogo}
                        className={styles.removeLogoBtn}
                      >
                        Remove Logo
                      </button>
                    </div>
                  )}
                </div>
              

              {/* Admin Details - Only show in create mode */}
              {!editingTenantId && (
                <>
                  <div className={styles.formGroup}>
                    <label className={styles.label}>
                      Admin First Name <span className={styles.required}></span>
                    </label>
                    <input
                      type="text"
                      name="TenantAdmin.FirstName"
                      value={formData.TenantAdmin.FirstName}
                      onChange={handleInputChange}
                      className={styles.input}
                      placeholder="Enter admin first name"
                      required
                    />
                  </div>

                  <div className={styles.formGroup}>
                    <label className={styles.label}>
                      Admin Last Name <span className={styles.required}></span>
                    </label>
                    <input
                      type="text"
                      name="TenantAdmin.LastName"
                      value={formData.TenantAdmin.LastName}
                      onChange={handleInputChange}
                      className={styles.input}
                      placeholder="Enter admin last name"
                      required
                    />
                  </div>

                  <div className={styles.formGroup}>
                    <label className={styles.label}>
                      Admin Email <span className={styles.required}></span>
                    </label>
                    <input
                      type="email"
                      name="TenantAdmin.Email"
                      value={formData.TenantAdmin.Email}
                      onChange={handleInputChange}
                      className={styles.input}
                      placeholder="Enter admin email"
                      required
                    />
                  </div>

                  <div className={styles.formGroup}>
                    <label className={styles.label}>
                      Admin Phone Number <span className={styles.required}></span>
                    </label>
                    <input
                      type="tel"
                      name="TenantAdmin.PhoneNumber"
                      value={formData.TenantAdmin.PhoneNumber}
                      onChange={handleInputChange}
                      className={styles.input}
                      placeholder="Enter admin phone (e.g., +1234567890)"
                      required
                    />
                  </div>

                  <div className={styles.formGroup}>
                    <label className={styles.label}>
                      Admin Password <span className={styles.required}></span>
                    </label>
                    <div className={styles.passwordContainer}>
                      <input
                        type={showPassword ? "text" : "password"}
                        name="TenantAdmin.Password"
                        value={formData.TenantAdmin.Password}
                        onChange={handleInputChange}
                        className={styles.input}
                        placeholder="Enter admin password"
                        required
                        minLength={6}
                      />
                      <button
                        type="button"
                        onClick={() => setShowPassword(!showPassword)}
                        className={styles.passwordToggle}
                      >
                        {showPassword ? '👁️' : '👁️‍🗨️'}
                      </button>
                    </div>
                  </div>
                </>
              )}
            </div>

            <div className={styles.formActions}>
              <button
                type="submit"
                className={styles.submitButton}
                disabled={isLoading || isUploadingLogo}
              >
                {isUploadingLogo
                  ? 'Uploading Logo...'
                  : isLoading
                    ? (isEditMode ? 'Updating...' : 'Creating...')
                    : (isEditMode ? 'Update Company' : 'Add Company')
                }
              </button>
              <button
                type="button"
                onClick={handleCancelEdit}
                className={styles.cancelButton}
                disabled={isLoading || isUploadingLogo}
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Projects List */}
      <div className={styles.companiesCard}>
        <div className={styles.companiesHeader}>
          <h3 className={styles.companiesTitle}>Companies List</h3>
        </div>
        <div className={styles.tableContainer}>
          <table className={styles.table}>
            <thead className={styles.tableHead}>
              <tr>
                <th className={styles.tableHeader}>Company</th>
                <th className={styles.tableHeader}>Company Name</th>
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
                    Loading Companies...
                  </td>
                </tr>
              ) : tenants.length === 0 ? (
                <tr>
                  <td colSpan={6} className={styles.tableCell}>
                    No Companies found. Create your first Company above.
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
                      </div>
                    </td>
                    <td className={styles.tableCell}>
                      <div className={styles.companyDetails}>
                        <div className={styles.companyName}>{tenant.CompanyName || tenant.Name}</div>
                        <div className={styles.companySlug}>{tenant.Slug}</div>
                      </div>
                    </td>
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
                        <button
                          type="button"
                          className={`${styles.actionButton} ${styles.viewButton}`}
                          onClick={() => handleViewProject(tenant)}
                        >
                          View
                        </button>
                        <button
                          type="button"
                          className={`${styles.actionButton} ${styles.editButton}`}
                          onClick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            handleEditClick(tenant.Id);
                          }}
                          disabled={isLoading}
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          className={`${styles.actionButton} ${styles.deleteButton}`}
                          onClick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            handleDeleteClick(tenant);
                          }}
                          disabled={isLoading}
                        >
                          Delete
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

      <DeleteConfirmationModal
        isOpen={showDeleteModal}
        onConfirm={handleConfirmDelete}
        onCancel={handleCancelDelete}
        companyName={tenantToDelete?.CompanyName || tenantToDelete?.Name || ''}
      />
    </div>
  );
}
