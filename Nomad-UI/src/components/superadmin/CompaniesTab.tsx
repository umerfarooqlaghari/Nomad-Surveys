/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import styles from './CompaniesTab.module.css';
import { tenantService, CreateTenantData, TenantListItem } from '@/services/tenantService';
import { authService } from '@/services/authService';

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
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});
  const [adminToastShown, setAdminToastShown] = useState(false);
  const [expandedSections, setExpandedSections] = useState({
    company: true,
    contact: false,
    admin: false
  });

  const toggleSection = (section: string) => {
    setExpandedSections(prev => ({
      ...prev,
      [section]: !prev[section as keyof typeof prev]
    }));
  };

  // Real-time validation for Admin Fields "All or Nothing" rule
  useEffect(() => {
    const errors: Record<string, string> = {};

    // Validate Slug first (always required)
    if (!formData.Slug) {
      errors['Slug'] = 'Slug is required';
    } else if (formData.Slug.length < 2) {
      errors['Slug'] = 'Slug must be at least 2 characters';
    } else if (!/^[a-z0-9-]+$/.test(formData.Slug)) {
      errors['Slug'] = 'Slug can only contain lowercase letters, numbers, and hyphens';
    }

    // Validate other required fields
    if (!formData.Name?.trim()) {
      errors['Name'] = 'Organization Name is required';
    }

    if (!formData.Company.Location?.trim()) {
      errors['Location'] = 'Location is required';
    }

    if (!formData.Company.Industry) {
      errors['Industry'] = 'Industry is required';
    }

    // Admin fields validation: "All or Nothing" rule
    // If TenantAdmin doesn't exist, no admin validation needed
    if (formData.TenantAdmin) {
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

      // Validate password if provided
      if (hasPassword) {
        const validation = authService.validatePassword(admin.Password!);
        if (!validation.isValid) {
          errors['Password'] = validation.errors.join('\n');
        }
      }
    }

    setValidationErrors(errors);

  }, [formData.TenantAdmin, formData.Slug, formData.Name, formData.Company.Location, formData.Company.Industry, isEditMode]);

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
        toast.error(error);
        console.error('Error loading tenants:', error);
      } else if (data) {
        setTenants(data);
      }
    } catch (err) {
      const errorMessage = 'Failed to load companies';
      setError(errorMessage);
      toast.error(errorMessage);
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
          [fieldName]: value,
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
  useEffect(() => {
    if (hasAdminValidationErrors()) {
      scrollToForm();
    }
  }, [validationErrors]); // keep focused on current validation state

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

  const hasBlockingValidation = Object.keys(validationErrors).length > 0 || hasAdminValidationErrors();
  const isSubmitVisuallyDisabled = hasBlockingValidation || isLoading;

  // 2. Create the reference
  const formTopRef = React.useRef<HTMLDivElement>(null);

  // 3. Create a helper function to scroll
  // const scrollToForm = () => {
  //   if (formTopRef.current) {
  //     window.scrollTo({ top: 0, behavior: 'smooth' });
  //   }
  // };
  const scrollToForm = () => {
    requestAnimationFrame(() => {
      window.scrollTo({ top: 0, behavior: 'smooth' });
      formTopRef.current?.scrollIntoView({
        behavior: 'smooth',
        block: 'start',
      });
    });
  };




  const handleSubmit = async (e?: React.FormEvent | React.MouseEvent) => {
    if (e) e.preventDefault();

    if (!token) {
      toast.error('Authentication required');
      return;
    }

    // Check for validation errors before submitting
    if (hasBlockingValidation) {
      toast.error('Please fix validation errors before creating');
      scrollToForm();
      return;
    }

    // Prepare data - set TenantAdmin to null if all fields are empty
    const submitData: CreateTenantData = {
      ...formData,
      TenantAdmin: isAdminEmpty(formData.TenantAdmin) ? null : formData.TenantAdmin,
    };

    // Validate form data
    const validation = tenantService.validateTenantData(submitData);
    if (!validation.isValid) {
      setError(validation.errors.join(', '));
      toast.error('Please fix the form errors');
      scrollToForm();
      return;
    }

    setError('');
    setIsLoading(true);

    try {
      const { data, error } = await tenantService.createTenant(submitData, token);

      if (error) {
        setError(error);
        toast.error(error);
        scrollToForm();
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
      scrollToForm();
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
          Password: '',
        } : null,
      });

      setEditingTenantId(tenantId);
      setIsEditMode(true);
      setShowForm(true);
      setTimeout(scrollToForm, 100); // Scroll to form when opening edit
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

    // Check for validation errors before submitting
    if (hasBlockingValidation) {
      toast.error('Please fix validation errors before updating');
      scrollToForm();
      return;
    }

    try {
      setIsLoading(true);
      setError('');

      // Determine if admin should be included - use "all or nothing" rule
      const adminIsEmpty = isAdminEmpty(formData.TenantAdmin);

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
        // If all admin fields are empty, set to null (no admin update)
        // Otherwise, include admin data (password can be null to keep current)
        TenantAdmin: adminIsEmpty ? null : (formData.TenantAdmin ? {
          FirstName: formData.TenantAdmin.FirstName,
          LastName: formData.TenantAdmin.LastName,
          Email: formData.TenantAdmin.Email,
          PhoneNumber: formData.TenantAdmin.PhoneNumber,
          Password: formData.TenantAdmin.Password || null
        } : null),
      };

      const { error } = await tenantService.updateTenant(editingTenantId, updateData, token);

      if (error) {
        setError(error);
        toast.error(error);
        scrollToForm();
        return;
      }

      toast.success('Company updated successfully!');
      handleCancelEdit();
      await loadTenants();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update company';
      setError(errorMessage);
      toast.error(errorMessage);
      scrollToForm();
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header} ref={formTopRef}>
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
              setTimeout(scrollToForm, 100); // Scroll to form when adding
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
                        className={`${styles.input} ${validationErrors['Name'] ? styles.inputError : ''}`}
                        placeholder="Enter organization name"
                      />
                      {validationErrors['Name'] && <span className={styles.errorText}>{validationErrors['Name']}</span>}
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
                      {validationErrors['Slug'] && <span className={styles.errorText}>{validationErrors['Slug']}</span>}
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
                        className={`${styles.input} ${validationErrors['Location'] ? styles.inputError : ''}`}
                        placeholder="Company location"
                      />
                      {validationErrors['Location'] && <span className={styles.errorText}>{validationErrors['Location']}</span>}
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
                        className={`${styles.select} ${validationErrors['Industry'] ? styles.inputError : ''}`}
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
                      {validationErrors['Industry'] && <span className={styles.errorText}>{validationErrors['Industry']}</span>}
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
                    {isEditMode ? (
                      <>
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
                        <div className={styles.fieldGroup} style={{ opacity: 0.7 }}>
                          <label className={styles.label}>
                            Note
                          </label>
                          <div style={{ fontSize: '0.875rem', color: '#6b7280', paddingTop: '0.5rem' }}>
                            Name, Email, and Phone will be automatically synced from the Admin account details below.
                          </div>
                        </div>
                      </>
                    )}
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
                  Admin Information
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
                  <div className={styles.formGrid}>
                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        First Name
                      </label>
                      <input
                        type="text"
                        value={formData.TenantAdmin?.FirstName ?? ''}
                        onChange={(e) => handleTenantAdminChange('FirstName', e.target.value)}
                        className={`${styles.input} ${validationErrors['FirstName'] ? styles.inputError : ''}`}
                        placeholder="First name"
                      />
                      {validationErrors['FirstName'] && <span className={styles.errorText}>{validationErrors['FirstName']}</span>}
                    </div>

                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        Last Name
                      </label>
                      <input
                        type="text"
                        value={formData.TenantAdmin?.LastName ?? ''}
                        onChange={(e) => handleTenantAdminChange('LastName', e.target.value)}
                        className={`${styles.input} ${validationErrors['LastName'] ? styles.inputError : ''}`}
                        placeholder="Last name"
                      />
                      {validationErrors['LastName'] && <span className={styles.errorText}>{validationErrors['LastName']}</span>}
                    </div>

                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        Email
                      </label>
                      <input
                        type="email"
                        value={formData.TenantAdmin?.Email ?? ''}
                        onChange={(e) => handleTenantAdminChange('Email', e.target.value)}
                        className={`${styles.input} ${validationErrors['Email'] ? styles.inputError : ''}`}
                        placeholder="admin@company.com"
                      />
                      {validationErrors['Email'] && <span className={styles.errorText}>{validationErrors['Email']}</span>}
                    </div>

                    <div className={styles.fieldGroup}>
                      <label className={styles.label}>
                        Phone Number
                      </label>
                      <input
                        type="tel"
                        value={formData.TenantAdmin?.PhoneNumber ?? ''}
                        onChange={(e) => handleTenantAdminChange('PhoneNumber', e.target.value)}
                        className={`${styles.input} ${validationErrors['PhoneNumber'] ? styles.inputError : ''}`}
                        placeholder="+1 (555) 123-4567"
                      />
                      {validationErrors['PhoneNumber'] && <span className={styles.errorText}>{validationErrors['PhoneNumber']}</span>}
                    </div>

                    <div className={styles.fieldGroup}>
                      <label className={`${styles.label} ${isEditMode ? '' : styles.required}`}>
                        Password
                      </label>
                      <div className={styles.passwordContainer}>
                        <input
                          type={showPassword ? "text" : "password"}
                          value={formData.TenantAdmin?.Password ?? ''}
                          onChange={(e) => handleTenantAdminChange('Password', e.target.value)}
                          minLength={6}
                          className={`${styles.input} ${validationErrors['Password'] ? styles.inputError : ''}`}
                          placeholder={isEditMode ? "Leave blank to keep current" : "Minimum 6 characters"}
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
                </div>
              )}
            </div>

          </form>

          {/* Action buttons outside form */}
          <div className={styles.formActions}>
            <button
              type="button"
              onClick={(evt) => {
                if (isLoading) return;
                if (hasBlockingValidation) {
                  toast.error('Please fix validation errors before submitting');
                  scrollToForm();
                  return;
                }
                if (isEditMode) {
                  handleUpdateSubmit(evt);
                } else {
                  handleSubmit(evt);
                }
              }}
              className={`${styles.submitButton} ${isSubmitVisuallyDisabled ? styles.submitButtonDisabled : ''}`}
              aria-disabled={isSubmitVisuallyDisabled}
              title={hasAdminValidationErrors() ? 'Please fill all admin fields or leave them all empty' : ''}
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
