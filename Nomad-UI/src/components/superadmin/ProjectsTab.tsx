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
  const formTopRef = useRef<HTMLDivElement>(null);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});
  const [adminToastShown, setAdminToastShown] = useState(false);
  const [expandedSections, setExpandedSections] = useState({
    company: true,
    contact: false,
    admin: false
  });
  const [hasSubmitted, setHasSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const toggleSection = (section: string) => {
    setExpandedSections(prev => ({
      ...prev,
      [section]: !prev[section as keyof typeof prev]
    }));
  };

  // Real-time validation for Admin Fields "All or Nothing" rule
  useEffect(() => {
    const errors: Record<string, string> = {};

    // Validate Slug (always required)
    // Only show "Required" error if user has attempted to submit
    if (!formData.Slug && hasSubmitted) {
      errors['Slug'] = 'Slug is required';
    } else if (formData.Slug && formData.Slug.length < 2) {
      errors['Slug'] = 'Slug must be at least 2 characters';
    } else if (formData.Slug && !/^[a-z0-9-]+$/.test(formData.Slug)) {
      errors['Slug'] = 'Slug can only contain lowercase letters, numbers, and hyphens';
    }

    // Validate Company Name (always required)
    if (!formData.Company.Name && hasSubmitted) {
      errors['Name'] = 'Company Name is required';
    }

    // Admin fields validation: "All or Nothing" rule
    // If TenantAdmin doesn't exist, no admin validation needed
    if (formData.TenantAdmin && !isEditMode) {
      const admin = formData.TenantAdmin;

      // Check which fields have values (treating empty strings as no value)
      const hasFirstName = !!admin.FirstName?.trim();
      const hasLastName = !!admin.LastName?.trim();
      const hasEmail = !!admin.Email?.trim();
      const hasPhoneNumber = !!admin.PhoneNumber?.trim();
      const hasPassword = !!admin.Password && admin.Password.length > 0;

      // Count filled fields (excluding password in edit mode since it's optional to keep current)
      const filledFields = [hasFirstName, hasLastName, hasEmail, hasPhoneNumber];
      const filledCount = filledFields.filter(Boolean).length;

      // In create mode, password is also required if any other field is filled
      // In edit mode, password is optional (leave blank to keep current)
      const hasAnyValue = filledCount > 0 || (!isEditMode && hasPassword);
      const allFieldsFilled = filledCount === 4 && (isEditMode || hasPassword);
      const noFieldsFilled = filledCount === 0 && !hasPassword;

      // "All or Nothing" rule: either all fields filled or none
      // Only trigger if user has provided at least one value
      if (hasAnyValue && !allFieldsFilled && !noFieldsFilled) {
        // Some fields are filled but not all - show validation errors for missing fields
        if (!hasFirstName) errors['FirstName'] = 'First Name is required when adding an admin';
        if (!hasLastName) errors['LastName'] = 'Last Name is required when adding an admin';

        if (!hasEmail) {
          errors['Email'] = 'Email is required when adding an admin';
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(admin.Email!)) {
          errors['Email'] = 'Invalid email format';
        }

        if (!hasPhoneNumber) errors['PhoneNumber'] = 'Phone Number is required when adding an admin';

        // Password required only in create mode
        if (!isEditMode && !hasPassword) {
          errors['Password'] = 'Password is required when adding an admin';
        }
      }

      // Validate email format if provided
      if (hasEmail && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(admin.Email!)) {
        errors['Email'] = 'Invalid email format';
      }

      // Validate password length if provided
      if (hasPassword && admin.Password!.length < 8) {
        errors['Password'] = 'Password must be at least 8 characters';
      }
    }

    setValidationErrors(errors);

  }, [formData.TenantAdmin, formData.Slug, formData.Company.Name, hasSubmitted, isEditMode]);

  // Helper function to check if all admin fields are empty
  const isAdminEmpty = (admin: CreateTenantData['TenantAdmin']): boolean => {
    if (!admin) return true;
    return !admin.FirstName?.trim() &&
      !admin.LastName?.trim() &&
      !admin.Email?.trim() &&
      !admin.PhoneNumber?.trim() &&
      (!admin.Password || admin.Password.length === 0);
  };

  // Helper to check if there are admin-related validation errors (for "all-or-nothing" rule)
  const hasAdminValidationErrors = (): boolean => {
    const adminFields = ['FirstName', 'LastName', 'Email', 'PhoneNumber', 'Password'];
    return adminFields.some(field => validationErrors[field]);
  };

  // Auto-scroll to form top when admin "all-or-nothing" validation kicks in
  // Removed per user request: only scroll on submit
  /*
  useEffect(() => {
    if (hasAdminValidationErrors()) {
      scrollToForm();
    }
  }, [validationErrors]);
  */

  // Show toast instead of inline banner for admin all-or-nothing rule
  useEffect(() => {
    const hasAdminErrors = hasAdminValidationErrors();
    if (hasAdminErrors && !adminToastShown) {
      toast.error('Admin fields are all-or-nothing: fill all admin fields or leave them all empty');
      setAdminToastShown(true);
    }
    if (!hasAdminErrors && adminToastShown) {
      setAdminToastShown(false);
    }
  }, [validationErrors, adminToastShown]); // eslint-disable-line react-hooks/exhaustive-deps

  const scrollToForm = () => {
    requestAnimationFrame(() => {
      window.scrollTo({ top: 0, behavior: 'smooth' });
      formTopRef.current?.scrollIntoView({
        behavior: 'smooth',
        block: 'start',
      });
    });
  };

  const hasBlockingValidation = Object.keys(validationErrors).length > 0 || hasAdminValidationErrors();
  const isSubmitVisuallyDisabled = hasBlockingValidation || isLoading || isUploadingLogo;

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
          [fieldName]: value
        }
      }));
    } else if (name.startsWith('TenantAdmin.')) {
      const fieldName = name.replace('TenantAdmin.', '');
      setFormData(prev => ({
        ...prev,
        TenantAdmin: prev.TenantAdmin ? { ...prev.TenantAdmin, [fieldName]: value } : { [fieldName]: value } as any
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

    // Check for blocking validation errors
    if (Object.keys(validationErrors).length > 0) {
      setHasSubmitted(true);
      toast.error('Please fix validation errors before creating');
      scrollToForm();
      return;
    }

    // Auto-set project name to company name
    const dataToSubmit = {
      ...formData,
      Name: formData.Company.Name
    };

    // Ensure state reflects submission attempt
    setHasSubmitted(true);

    // Validate form data
    const validation = tenantService.validateTenantData(dataToSubmit);
    if (!validation.isValid) {
      setError(validation.errors.join(', '));
      toast.error('Please fix the form errors');
      scrollToForm();
      return;
    }

    // Check if logo is still uploading
    if (isUploadingLogo) {
      toast.error('Please wait for logo upload to complete');
      return;
    }

    // Check if logo file is selected but not uploaded (safety check)
    if (logoFile && !dataToSubmit.Company.LogoUrl) {
      toast.error('Logo is still processing. Please wait.');
      return;
    }

    setIsSubmitting(true);
    setIsLoading(true);
    setError('');

    try {
      const response = await tenantService.createTenant(dataToSubmit, token);

      if (response.error) {
        setError(response.error);
        toast.error(response.error);
        scrollToForm();
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
      scrollToForm();
    } finally {
      setIsSubmitting(false);
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
          NumberOfEmployees: data.Company?.NumberOfEmployees?.toString() ?? null,
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
          Password: '', // Password is never returned from API for security
        } : null,
      });

      if (data.Company?.LogoUrl) {
        setLogoPreview(data.Company.LogoUrl);
      } else {
        setLogoPreview('');
      }

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

    if (Object.keys(validationErrors).length > 0) {
      setHasSubmitted(true);
      toast.error('Please fix validation errors before updating');
      scrollToForm();
      return;
    }

    // Since validation is partially based on things like admin password (which we don't have on edit),
    // we should be careful here. However, tenantService.validateTenantData might need 
    // a variant for updates or we can just validate the company fields locally.

    // For simplicity, we'll validate the basic requirements
    if (!formData.Company.Name.trim() || !formData.Slug.trim()) {
      setHasSubmitted(true);
      toast.error('Company name and slug are required');
      scrollToForm();
      return;
    }

    setIsSubmitting(true);
    setIsLoading(true);
    setHasSubmitted(true);
    setError('');

    try {
      const updateData = {
        ...formData,
        Name: formData.Company.Name, // Auto-set project name to company name
        TenantAdmin: null, // Ensure TenantAdmin is null for updates
      };

      const response = await tenantService.updateTenant(editingTenantId, updateData, token);

      if (response.error) {
        setError(response.error);
        toast.error(response.error);
        scrollToForm();
        return;
      }

      toast.success('Company updated successfully!');
      handleCancelEdit();
      await loadTenants();
    } catch (err: any) {
      const errorMessage = err.message || 'Failed to update company';
      setError(errorMessage);
      toast.error(errorMessage);
      scrollToForm();
    } finally {
      setIsSubmitting(false);
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      {/* Loading Overlay */}
      {(isSubmitting || isUploadingLogo) && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(0, 0, 0, 0.5)',
          backdropFilter: 'blur(4px)',
          zIndex: 9999,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '1rem'
        }}>
          <div style={{
            backgroundColor: 'white',
            borderRadius: '1rem',
            boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.25)',
            padding: '2rem',
            maxWidth: '24rem',
            width: '100%',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            textAlign: 'center'
          }}>
            <div style={{ position: 'relative', width: '4rem', height: '4rem', marginBottom: '1.5rem' }}>
              <div style={{ position: 'absolute', inset: 0, border: '4px solid #e0e7ff', borderRadius: '9999px' }}></div>
              <div style={{
                position: 'absolute',
                inset: 0,
                border: '4px solid #4f46e5',
                borderRadius: '9999px',
                borderTopColor: 'transparent',
                animation: 'spin 1s linear infinite'
              }}></div>
            </div>
            <style jsx>{`
              @keyframes spin {
                from { transform: rotate(0deg); }
                to { transform: rotate(360deg); }
              }
            `}</style>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 'bold', color: '#111827', marginBottom: '0.5rem' }}>
              {isUploadingLogo ? 'Uploading Logo' : 'Processing'}
            </h3>
            <p style={{ color: '#4b5563' }}>
              Please wait while we {isUploadingLogo ? 'upload your file' : 'process your request'}. This may take a moment.
            </p>
          </div>
        </div>
      )}
      {/* Header */}
      <div className={styles.header} ref={formTopRef}>
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
              // Wait for render then scroll
              setTimeout(scrollToForm, 100);
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
          <form onSubmit={isEditMode ? handleUpdateSubmit : handleSubmit} className={styles.form} autoComplete="off">

            {/* Section 1: Company Information */}
            <div className={styles.collapsibleSection} style={{ borderColor: (validationErrors['Slug'] || validationErrors['Name']) ? '#fecaca' : undefined }}>
              <div
                className={`${styles.collapsibleHeader} ${expandedSections.company ? styles.collapsibleHeaderExpanded : ''}`}
                onClick={() => toggleSection('company')}
              >
                <div className={styles.collapsibleTitle}>
                  <div className={styles.sectionIcon}>
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M3 21h18M5 21V7l8-4 8 4v14M8 21v-8h8v8" />
                    </svg>
                  </div>
                  Company Information
                </div>
                <svg
                  className={`${styles.chevron} ${expandedSections.company ? styles.chevronOpen : ''}`}
                  viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
                >
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </div>

              {expandedSections.company && (
                <div className={styles.collapsibleContent}>
                  <div className={styles.formGrid}>
                    {/* Company Name */}
                    <div className={styles.fieldGroup}>
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
                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        Company Code <span className={styles.required}></span>
                      </label>
                      <input
                        type="text"
                        name="Slug"
                        value={formData.Slug}
                        onChange={handleInputChange}
                        className={styles.input}
                        placeholder="Enter company Code (e.g., company-name)"
                        required
                      />
                      {validationErrors['Slug'] && <span className={styles.errorText}>{validationErrors['Slug']}</span>}
                    </div>

                    {/* Number of Employees */}
                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        Number of Employees
                      </label>
                      <select
                        name="Company.NumberOfEmployees"
                        value={formData.Company.NumberOfEmployees ?? ''}
                        onChange={handleInputChange}
                        className={styles.select}
                      >
                        <option value="">Select Range</option>
                        <option value="0-100">0-100</option>
                        <option value="101-250">101-250</option>
                        <option value="251-500">251-500</option>
                        <option value="501-1000">501-1000</option>
                        <option value="1000+">1000+</option>
                      </select>
                    </div>

                    {/* Industry */}
                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        Industry <span className={styles.required}></span>
                      </label>
                      <select
                        name="Company.Industry"
                        value={formData.Company.Industry}
                        onChange={handleInputChange}
                        className={styles.select}
                        required
                      >
                        <option value="">Select Industry</option>
                        <option value="Technology">Technology</option>
                        <option value="Healthcare">Healthcare</option>
                        <option value="Finance">Finance</option>
                        <option value="Education">Education</option>
                        <option value="Manufacturing">Manufacturing</option>
                        <option value="Retail">Retail</option>
                        <option value="Real Estate">Real Estate</option>
                        <option value="Hospitality">Hospitality</option>
                        <option value="Transportation">Transportation</option>
                        <option value="Energy">Energy</option>
                        <option value="Media & Entertainment">Media & Entertainment</option>
                        <option value="Telecommunications">Telecommunications</option>
                        <option value="Agriculture">Agriculture</option>
                        <option value="Construction">Construction</option>
                        <option value="Legal">Legal</option>
                        <option value="Consulting">Consulting</option>
                        <option value="Non-Profit">Non-Profit</option>
                        <option value="Government">Government</option>
                        <option value="Other">Other</option>
                      </select>
                    </div>

                    {/* Location */}
                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        City <span className={styles.required}></span>
                      </label>
                      <input
                        type="text"
                        name="Company.Location"
                        value={formData.Company.Location}
                        onChange={handleInputChange}
                        className={styles.input}
                        placeholder="Enter City"
                        required
                      />
                    </div>

                    {/* Company Logo */}
                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>Company Logo</label>
                      <div className={styles.logoUploadContainer}>
                        {!logoPreview && (
                          <input
                            type="file"
                            ref={fileInputRef}
                            accept="image/*"
                            onChange={handleLogoUpload}
                            className={styles.input}
                            disabled={isUploadingLogo || isLoading}
                          />
                        )}
                        {isUploadingLogo && (
                          <p style={{ marginTop: '8px', color: '#7c3aed', fontSize: '0.875rem' }}>Uploading logo...</p>
                        )}
                        {logoPreview && (
                          <div className={styles.logoPreviewWrapper}>
                            <img
                              src={logoPreview}
                              alt="Logo preview"
                              className={styles.logoPreviewImg}
                              onError={(e) => {
                                e.currentTarget.style.display = 'none';
                                // Optional: show placeholder or error text
                              }}
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
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Section 2: Contact Person Information */}
            <div className={styles.collapsibleSection}>
              <div
                className={`${styles.collapsibleHeader} ${expandedSections.contact ? styles.collapsibleHeaderExpanded : ''}`}
                onClick={() => toggleSection('contact')}
              >
                <div className={styles.collapsibleTitle}>
                  <div className={styles.sectionIcon}>
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                      <circle cx="12" cy="7" r="4"></circle>
                    </svg>
                  </div>
                  Contact Person Information
                </div>
                <svg
                  className={`${styles.chevron} ${expandedSections.contact ? styles.chevronOpen : ''}`}
                  viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
                >
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </div>

              {expandedSections.contact && (
                <div className={styles.collapsibleContent}>
                  <div className={styles.formGrid}>
                    {/* Contact Person Name */}
                    <div className={styles.fieldGroup}>
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
                    <div className={styles.fieldGroup}>
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
                        placeholder="Enter contact person role"
                      />
                    </div>

                    {/* Contact Person Phone */}
                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        Contact Person Phone
                      </label>
                      <input
                        type="tel"
                        name="Company.ContactPersonPhone"
                        value={formData.Company.ContactPersonPhone ?? ''}
                        onChange={handleInputChange}
                        className={styles.input}
                        placeholder="Enter contact person phone"
                      />
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Section 3: Admin Information */}
            <div className={styles.collapsibleSection} style={{ borderColor: hasAdminValidationErrors() ? '#fecaca' : undefined }}>
              <div
                className={`${styles.collapsibleHeader} ${expandedSections.admin ? styles.collapsibleHeaderExpanded : ''}`}
                onClick={() => toggleSection('admin')}
              >
                <div className={styles.collapsibleTitle}>
                  <div className={styles.sectionIcon}>
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
                    </svg>
                  </div>
                  Admin Information {isEditMode && <span style={{ fontSize: '0.8em', fontWeight: 'normal', marginLeft: '8px', color: '#6b7280' }}>(Optional)</span>}
                </div>
                <svg
                  className={`${styles.chevron} ${expandedSections.admin ? styles.chevronOpen : ''}`}
                  viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
                >
                  <polyline points="6 9 12 15 18 9"></polyline>
                </svg>
              </div>

              {expandedSections.admin && (
                <div className={styles.collapsibleContent}>
                  {isEditMode && <div style={{ marginBottom: '1rem', fontSize: '0.9rem', color: '#6b7280' }}>Leaves fields blank to keep current admin details unchanged.</div>}
                  {/* Admin Details - Only show in create mode or allow editing in update (logic handled by isEditMode check in fields? No, original hid it completely. But typically users want to update admin. I'll expose it but keep logic clean) */}
                  {/* Original code HID it during edit: {!editingTenantId && (...)} */}
                  {/* User asked for "Admin toasts" which implies admin editing or creating. I will Show it always but handle the "null" sending on update if empty? */}
                  {/* Actually, the handleUpdateSubmit code logic I saw sets TenantAdmin: null. So updating admin might not be supported by the backend update endpoint here? */}
                  {/* Code: Name: formData.Company.Name, TenantAdmin: null. */}
                  {/* So for Edit Mode, this section might be non-functional or just hidden? */}
                  {/* I will follow the original logic: only show if !editingTenantId (Create Mode). */}

                  {!editingTenantId ? (
                    <div className={styles.formGrid}>
                      <div className={styles.fieldGroup}>
                        <label className={styles.label}>
                          Admin First Name
                        </label>
                        <input
                          type="text"
                          name="TenantAdmin.FirstName"
                          value={formData.TenantAdmin?.FirstName ?? ''}
                          onChange={handleInputChange}
                          className={`${styles.input} ${validationErrors['FirstName'] ? styles.inputError : ''}`}
                          placeholder="Enter admin first name"
                        />
                        {validationErrors['FirstName'] && <span className={styles.errorText}>{validationErrors['FirstName']}</span>}
                      </div>

                      <div className={styles.fieldGroup}>
                        <label className={styles.label}>
                          Admin Last Name
                        </label>
                        <input
                          type="text"
                          name="TenantAdmin.LastName"
                          value={formData.TenantAdmin?.LastName ?? ''}
                          onChange={handleInputChange}
                          className={`${styles.input} ${validationErrors['LastName'] ? styles.inputError : ''}`}
                          placeholder="Enter admin last name"
                        />
                        {validationErrors['LastName'] && <span className={styles.errorText}>{validationErrors['LastName']}</span>}
                      </div>

                      <div className={styles.fieldGroup}>
                        <label className={styles.label}>
                          Admin Email
                        </label>
                        <input
                          type="email"
                          name="TenantAdmin.Email"
                          value={formData.TenantAdmin?.Email ?? ''}
                          onChange={handleInputChange}
                          className={`${styles.input} ${validationErrors['Email'] ? styles.inputError : ''}`}
                          placeholder="Enter admin email"
                          autoComplete="new-password"
                        />
                        {validationErrors['Email'] && <span className={styles.errorText}>{validationErrors['Email']}</span>}
                      </div>

                      <div className={styles.fieldGroup}>
                        <label className={styles.label}>
                          Admin Phone Number
                        </label>
                        <input
                          type="tel"
                          name="TenantAdmin.PhoneNumber"
                          value={formData.TenantAdmin?.PhoneNumber ?? ''}
                          onChange={handleInputChange}
                          className={`${styles.input} ${validationErrors['PhoneNumber'] ? styles.inputError : ''}`}
                          placeholder="Enter admin phone (e.g., +1234567890)"
                          autoComplete="new-password"
                        />
                        {validationErrors['PhoneNumber'] && <span className={styles.errorText}>{validationErrors['PhoneNumber']}</span>}
                      </div>

                      <div className={styles.fieldGroup}>
                        <label className={styles.label}>
                          Admin Password
                        </label>
                        <div className={styles.passwordContainer}>
                          <input
                            type={showPassword ? "text" : "password"}
                            name="TenantAdmin.Password"
                            value={formData.TenantAdmin?.Password ?? ''}
                            onChange={handleInputChange}
                            className={`${styles.input} ${validationErrors['Password'] ? styles.inputError : ''}`}
                            placeholder="Enter admin password"
                            minLength={6}
                            autoComplete="new-password"
                          />
                          <button
                            type="button"
                            onClick={() => setShowPassword(!showPassword)}
                            className={styles.passwordToggle}
                          >
                            {showPassword ? 'HIDE' : 'SHOW'}
                          </button>
                        </div>
                        {validationErrors['Password'] && <span className={styles.errorText}>{validationErrors['Password']}</span>}
                      </div>
                    </div>
                  ) : (
                    <div style={{ padding: '0.5rem', color: '#6b7280', fontStyle: 'italic' }}>
                      Admin details cannot be modified here. Please manage users in the Users tab.
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className={styles.formActions}>
              <button
                type="submit"
                className={styles.submitButton}
                disabled={isSubmitVisuallyDisabled}
                title={hasBlockingValidation ? "Please fix form errors" : ""}
                style={hasBlockingValidation ? { opacity: 0.7, cursor: 'not-allowed' } : {}}
              >
                {isUploadingLogo
                  ? 'Uploading Logo...'
                  : isLoading
                    ? (isEditMode ? 'Updating...' : 'Creating...')
                    : (isEditMode ? 'Update Company' : 'Add Company')
                }
              </button>
              {/* <button
                type="button"
                onClick={handleCancelEdit}
                className={styles.cancelButton}
                disabled={isLoading || isUploadingLogo}
              >
                Cancel
              </button> */}
            </div>
          </form>
        </div>
      )}

      {/* Projects List */}
      <div className={styles.companiesCard}>
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
