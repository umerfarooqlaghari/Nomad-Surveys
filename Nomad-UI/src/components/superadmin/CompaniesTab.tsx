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

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h2>Companies</h2>
          <p>Manage organization onboarding and information</p>
        </div>
        <button
          onClick={() => setShowForm(!showForm)}
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
          <h3 className={styles.formTitle}>Organization Onboarding</h3>
          <form onSubmit={handleSubmit} className={styles.form}>
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
                    Logo URL
                  </label>
                  <input
                    type="url"
                    name="company.logoUrl"
                    value={formData.company.logoUrl}
                    onChange={handleInputChange}
                    className={styles.input}
                    placeholder="https://example.com/logo.png"
                  />
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
                    required
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
                    required
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
                    required
                    className={styles.input}
                    placeholder="admin@company.com"
                  />
                </div>

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
              </div>
            </div>

            <div className={styles.formActions}>
              <button
                type="button"
                onClick={() => setShowForm(false)}
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
                {isLoading ? 'Creating...' : 'Create Company'}
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
                  <tr key={tenant.id} className={styles.tableRow}>
                    <td className={styles.tableCell}>
                      <div className={styles.companyInfo}>
                        <div className={styles.logoContainer}>
                          <div className={styles.logoPlaceholder}>LOGO</div>
                        </div>
                        <div className={styles.companyDetails}>
                          <div className={styles.companyName}>{tenant.companyName || tenant.name}</div>
                          <div className={styles.companySlug}>{tenant.name}</div>
                        </div>
                      </div>
                    </td>
                    <td className={styles.tableCell}>{tenant.slug}</td>
                    <td className={styles.tableCell}>{tenant.userCount}</td>
                    <td className={styles.tableCell}>
                      <span className={`${styles.statusBadge} ${
                        tenant.isActive ? styles.statusActive : styles.statusInactive
                      }`}>
                        {tenant.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className={styles.tableCell}>
                      {new Date(tenant.createdAt).toLocaleDateString()}
                    </td>
                    <td className={styles.tableCell}>
                      <div className={styles.actionButtons}>
                        <button className={`${styles.actionButton} ${styles.viewButton}`}>
                          View
                        </button>
                        <button className={`${styles.actionButton} ${styles.editButton}`}>
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
