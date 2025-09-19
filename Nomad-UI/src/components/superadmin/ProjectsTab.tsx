/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import styles from './CompaniesTab.module.css';
import { tenantService, CreateTenantData, TenantListItem } from '@/services/tenantService';

export default function ProjectsTab() {
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
      setError(err.message || 'Failed to load projects');
      toast.error('Failed to load projects');
    } finally {
      setIsLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;

    // Handle nested object updates
    if (name.startsWith('company.')) {
      const fieldName = name.replace('company.', '');
      setFormData(prev => ({
        ...prev,
        company: { ...prev.company, [fieldName]: value }
      }));
    } else if (name.startsWith('tenantAdmin.')) {
      const fieldName = name.replace('tenantAdmin.', '');
      setFormData(prev => ({
        ...prev,
        tenantAdmin: { ...prev.tenantAdmin, [fieldName]: value }
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
    if (!token) return;

    setIsLoading(true);
    setError('');

    try {
      await tenantService.createTenant(formData, token);
      toast.success('Project created successfully!');
      setShowForm(false);
      setFormData(tenantService.getDefaultFormData());
      await loadTenants(); // Reload the list
    } catch (err: any) {
      console.error('Error creating tenant:', err);
      const errorMessage = err.message || 'Failed to create project';
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

  return (
    <div className={styles.container}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerContent}>
          <h2>Projects</h2>
          <p>Manage project onboarding and information</p>
        </div>
        <button
          onClick={() => setShowForm(!showForm)}
          className={styles.addButton}
          disabled={isLoading}
        >
          {showForm ? 'Cancel' : 'Add Project'}
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
            <h3 className={styles.formTitle}>Add New Project</h3>
          </div>
          <form onSubmit={handleSubmit} className={styles.form}>
            <div className={styles.formGrid}>
              {/* Project Name */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Project Name <span className={styles.required}>*</span>
                </label>
                <input
                  type="text"
                  name="name"
                  value={formData.name}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter project name"
                  required
                />
              </div>

              {/* Company Name */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Company Name <span className={styles.required}>*</span>
                </label>
                <input
                  type="text"
                  name="company.name"
                  value={formData.company.name}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter company name"
                  required
                />
              </div>

              {/* Admin Details */}
              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Admin First Name <span className={styles.required}>*</span>
                </label>
                <input
                  type="text"
                  name="tenantAdmin.firstName"
                  value={formData.tenantAdmin.firstName}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter admin first name"
                  required
                />
              </div>

              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Admin Last Name <span className={styles.required}>*</span>
                </label>
                <input
                  type="text"
                  name="tenantAdmin.lastName"
                  value={formData.tenantAdmin.lastName}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter admin last name"
                  required
                />
              </div>

              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Admin Email <span className={styles.required}>*</span>
                </label>
                <input
                  type="email"
                  name="tenantAdmin.email"
                  value={formData.tenantAdmin.email}
                  onChange={handleInputChange}
                  className={styles.input}
                  placeholder="Enter admin email"
                  required
                />
              </div>

              <div className={styles.formGroup}>
                <label className={styles.label}>
                  Admin Password <span className={styles.required}>*</span>
                </label>
                <div className={styles.passwordContainer}>
                  <input
                    type={showPassword ? "text" : "password"}
                    name="tenantAdmin.password"
                    value={formData.tenantAdmin.password}
                    onChange={handleInputChange}
                    className={styles.input}
                    placeholder="Enter admin password"
                    required
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
                {isLoading ? 'Creating...' : 'Create Project'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Projects List */}
      <div className={styles.companiesCard}>
        <div className={styles.companiesHeader}>
          <h3 className={styles.companiesTitle}>Projects List</h3>
        </div>
        <div className={styles.tableContainer}>
          <table className={styles.table}>
            <thead className={styles.tableHead}>
              <tr>
                <th className={styles.tableHeader}>Project</th>
                <th className={styles.tableHeader}>Project Slug</th>
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
                    Loading projects...
                  </td>
                </tr>
              ) : tenants.length === 0 ? (
                <tr>
                  <td colSpan={6} className={styles.tableCell}>
                    No projects found. Create your first project above.
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
                          {/* <div className={styles.companyName}>{tenant.companyName || tenant.Name}</div>
                          <div className={styles.companySlug}>{tenant.Slug}</div> */}
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
                        <button 
                          className={`${styles.actionButton} ${styles.viewButton}`}
                          onClick={() => handleViewProject(tenant)}
                        >
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
