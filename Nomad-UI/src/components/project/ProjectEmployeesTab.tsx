/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import { employeeService, Employee, EmployeeListResponse, CreateEmployeeRequest, UpdateEmployeeRequest, AdditionalAttribute } from '@/services/employeeService';

interface ProjectEmployeesTabProps {
  projectSlug: string;
}

export default function ProjectEmployeesTab({ projectSlug }: ProjectEmployeesTabProps) {
  const { token } = useAuth();
  const [employees, setEmployees] = useState<EmployeeListResponse[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null);
  const [formData, setFormData] = useState({
    FirstName: '',
    LastName: '',
    Email: '',
    Number: '',
    EmployeeId: '',
    CompanyName: '',
    Designation: '',
    Department: '',
    Tenure: '',
    Grade: '',
    Gender: '',
    ManagerId: '',
  });
  const [additionalAttributes, setAdditionalAttributes] = useState<AdditionalAttribute[]>([]);

  // Search and filter state
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<'all' | 'active' | 'inactive'>('all');
  const [filterDepartment, setFilterDepartment] = useState<string>('all');

  useEffect(() => {
    if (token) {
      fetchEmployees();
    }
  }, [token, projectSlug]);

  const fetchEmployees = async () => {
    if (!token) return;

    setIsLoading(true);
    try {
      const { data, error } = await employeeService.getEmployees(projectSlug, token);
      
      if (error) {
        toast.error(error);
        return;
      }

      if (data) {
        setEmployees(data);
      }
    } catch (error) {
      console.error('Error fetching employees:', error);
      toast.error('Failed to load employees');
    } finally {
      setIsLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const resetForm = () => {
    setFormData({
      FirstName: '',
      LastName: '',
      Email: '',
      Number: '',
      EmployeeId: '',
      CompanyName: '',
      Designation: '',
      Department: '',
      Tenure: '',
      Grade: '',
      Gender: '',
      ManagerId: '',
    });
    setAdditionalAttributes([]);
    setEditingEmployee(null);
    setShowAddForm(false);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!token) {
      toast.error('Authentication required');
      return;
    }

    try {
      const employeeData: CreateEmployeeRequest | UpdateEmployeeRequest = {
        FirstName: formData.FirstName,
        LastName: formData.LastName,
        Email: formData.Email,
        Number: formData.Number || undefined,
        EmployeeId: formData.EmployeeId,
        CompanyName: formData.CompanyName || undefined,
        Designation: formData.Designation || undefined,
        Department: formData.Department || undefined,
        Tenure: formData.Tenure ? parseInt(formData.Tenure) : undefined,
        Grade: formData.Grade || undefined,
        Gender: formData.Gender || undefined,
        ManagerId: formData.ManagerId || undefined,
        MoreInfo: additionalAttributes.length > 0 ? additionalAttributes : undefined,
      };

      if (editingEmployee) {
        // Update existing employee
        const { data, error } = await employeeService.updateEmployee(
          projectSlug,
          editingEmployee.Id,
          employeeData as UpdateEmployeeRequest,
          token
        );

        if (error) {
          toast.error(error);
          return;
        }

        if (data) {
          toast.success('Employee updated successfully');
          await fetchEmployees();
          resetForm();
        }
      } else {
        // Create new employee
        const { data, error } = await employeeService.createEmployee(
          projectSlug,
          employeeData as CreateEmployeeRequest,
          token
        );

        if (error) {
          toast.error(error);
          return;
        }

        if (data) {
          toast.success('Employee created successfully');
          await fetchEmployees();
          resetForm();
        }
      }
    } catch (error) {
      console.error('Error saving employee:', error);
      toast.error('Failed to save employee');
    }
  };

  const handleEdit = async (employee: EmployeeListResponse) => {
    if (!token) return;

    // Fetch full employee details to get MoreInfo
    const { data: fullEmployee, error } = await employeeService.getEmployeeById(projectSlug, employee.Id, token);

    if (error || !fullEmployee) {
      toast.error('Failed to load employee details');
      return;
    }

    setEditingEmployee(fullEmployee);
    setFormData({
      FirstName: fullEmployee.FirstName,
      LastName: fullEmployee.LastName,
      Email: fullEmployee.Email,
      Number: fullEmployee.Number || '',
      EmployeeId: fullEmployee.EmployeeId,
      CompanyName: fullEmployee.CompanyName || '',
      Designation: fullEmployee.Designation || '',
      Department: fullEmployee.Department || '',
      Tenure: fullEmployee.Tenure?.toString() || '',
      Grade: fullEmployee.Grade || '',
      Gender: fullEmployee.Gender || '',
      ManagerId: fullEmployee.ManagerId || '',
    });
    setAdditionalAttributes(fullEmployee.MoreInfo || []);
    setShowAddForm(true);
  };

  const handleDelete = async (employeeId: string) => {
    if (!token) {
      toast.error('Authentication required');
      return;
    }

    if (!confirm('Are you sure you want to delete this employee?')) {
      return;
    }

    try {
      const { success, error } = await employeeService.deleteEmployee(projectSlug, employeeId, token);

      if (error) {
        toast.error(error);
        return;
      }

      if (success) {
        toast.success('Employee deleted successfully');
        await fetchEmployees();
      }
    } catch (error) {
      console.error('Error deleting employee:', error);
      toast.error('Failed to delete employee');
    }
  };

  const downloadTemplate = () => {
    const template = employeeService.generateCSVTemplate();
    const blob = new Blob([template], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'employees_template.csv';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
    toast.success('Template downloaded successfully');
  };

  const handleBulkImport = async (event: React.ChangeEvent<HTMLInputElement>) => {
    console.log('🔵 [EMPLOYEES] handleBulkImport TRIGGERED');
    
    const file = event.target.files?.[0];
    console.log('📁 [EMPLOYEES] File object:', file);

    if (!file) {
      console.log('❌ [EMPLOYEES] No file selected');
      return;
    }

    if (!token) {
      console.log('❌ [EMPLOYEES] No token available');
      toast.error('Authentication token not found. Please log in again.');
      return;
    }

    // Reset the file input
    event.target.value = '';

    const loadingToast = toast.loading('Processing CSV file...');

    try {
      // Read file content
      console.log('📖 [EMPLOYEES] Reading file...');
      const fileContent = await file.text();
      console.log('📄 [EMPLOYEES] File content length:', fileContent.length);

      // Parse CSV
      console.log('🔄 [EMPLOYEES] Parsing CSV...');
      const { employees, errors } = employeeService.parseCSV(fileContent);
      console.log('✅ [EMPLOYEES] Parsed:', employees.length, 'employees,', errors.length, 'errors');

      // Show parsing errors but don't stop processing valid entries
      if (errors.length > 0) {
        const errorSummary = `⚠️ ${errors.length} row(s) skipped due to errors:\n${errors.slice(0, 3).join('\n')}${errors.length > 3 ? `\n... and ${errors.length - 3} more` : ''}`;
        toast.error(errorSummary, { duration: 10000 });
        console.error('CSV parsing errors:', errors);
      }

      // Only stop if there are no valid employees to process
      if (employees.length === 0) {
        toast.dismiss(loadingToast);
        toast.error('No valid employees found in CSV file');
        return;
      }

      // Update loading message to show we're uploading
      toast.loading(`Uploading ${employees.length} valid employee(s)...`, { id: loadingToast });

      // Bulk create employees
      console.log('📤 [EMPLOYEES] Sending bulk create request...');
      const { data, error } = await employeeService.bulkCreateEmployees(
        projectSlug,
        { Employees: employees },
        token
      );

      // Dismiss loading toast after API call completes
      toast.dismiss(loadingToast);

      if (error) {
        toast.error(`Failed to import employees: ${error}`);
        return;
      }

      if (data) {
        // Build comprehensive summary
        const totalRows = employees.length + errors.length;
        const successMessage = ` Import Summary:\n` +
          `• Total rows in CSV: ${totalRows}\n` +
          `• Successfully created: ${data.SuccessfullyCreated}\n` +
          `• Updated: ${data.UpdatedCount}\n` +
          `• Skipped during parsing: ${errors.length}\n` +
          `• Failed during upload: ${data.Failed}`;

        if (data.Failed > 0 && data.Errors.length > 0) {
          const errorDetails = data.Errors.slice(0, 3).join('\n');
          toast.error(`${successMessage}\n\n❌ Upload Errors:\n${errorDetails}${data.Errors.length > 3 ? `\n... and ${data.Errors.length - 3} more` : ''}`, { duration: 10000 });
        } else if (errors.length > 0) {
          // Some rows were skipped during parsing but upload was successful
          toast.success(`${successMessage}\n\n Valid entries were processed successfully`, { duration: 8000 });
        } else {
          // Everything succeeded
          toast.success(successMessage, { duration: 10000 });
        }

        await fetchEmployees();
      }
    } catch (error: any) {
      toast.dismiss(loadingToast);
      console.error('❌ [EMPLOYEES] Error during bulk import:', error);
      toast.error(`Failed to import employees: ${error.message || 'Unknown error'}`);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white shadow rounded-lg p-6">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text-2xl font-bold text-gray-900">Employees</h2>
            <p className="text-gray-600">Manage employees in your organization</p>
          </div>
          <div className="flex space-x-3">
            <button
              onClick={downloadTemplate}
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium flex items-center gap-2"
              title="Download CSV template with sample data"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              Download Template
            </button>
            <label className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium cursor-pointer flex items-center gap-2">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
              </svg>
              Import CSV
              <input
                type="file"
                accept=".csv"
                onChange={handleBulkImport}
                className="hidden"
              />
            </label>
            <button
              onClick={() => setShowAddForm(!showAddForm)}
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              {showAddForm ? 'Cancel' : '+ Add Employee'}
            </button>
          </div>
        </div>

        {/* Add/Edit Form */}
        {showAddForm && (
          <form onSubmit={handleSubmit} className="bg-gray-50 p-6 rounded-lg space-y-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">
              {editingEmployee ? 'Edit Employee' : 'Add New Employee'}
            </h3>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* First Name */}
              <div>
                <label className="block text-sm font-medium text-black mb-1">
                  First Name <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  name="FirstName"
                  value={formData.FirstName}
                  onChange={handleInputChange}
                  required
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="John"
                />
              </div>

              {/* Last Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Last Name <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  name="LastName"
                  value={formData.LastName}
                  onChange={handleInputChange}
                  required
                  className="w-full px-3 py-2  text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="Doe"
                />
              </div>

              {/* Email */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Email <span className="text-red-500">*</span>
                </label>
                <input
                  type="email"
                  name="Email"
                  value={formData.Email}
                  onChange={handleInputChange}
                  required
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="john.doe@example.com"
                />
              </div>

              {/* Employee ID */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Employee ID <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  name="EmployeeId"
                  value={formData.EmployeeId}
                  onChange={handleInputChange}
                  required
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="EMP001"
                />
              </div>

              {/* Phone Number */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Phone Number
                </label>
                <input
                  type="text"
                  name="Number"
                  value={formData.Number}
                  onChange={handleInputChange}
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="+1234567890"
                />
              </div>

              {/* Company Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Company Name
                </label>
                <input
                  type="text"
                  name="CompanyName"
                  value={formData.CompanyName}
                  onChange={handleInputChange}
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="Acme Corp"
                />
              </div>

              {/* Designation */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Designation
                </label>
                <input
                  type="text"
                  name="Designation"
                  value={formData.Designation}
                  onChange={handleInputChange}
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="Software Engineer"
                />
              </div>

              {/* Department */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Department
                </label>
                <input
                  type="text"
                  name="Department"
                  value={formData.Department}
                  onChange={handleInputChange}
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="Engineering"
                />
              </div>

              {/* Tenure */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Tenure (years)
                </label>
                <input
                  type="number"
                  name="Tenure"
                  value={formData.Tenure}
                  onChange={handleInputChange}
                  min="0"
                  max="100"
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="5"
                />
              </div>

              {/* Grade */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Grade
                </label>
                <input
                  type="text"
                  name="Grade"
                  value={formData.Grade}
                  onChange={handleInputChange}
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="Senior"
                />
              </div>

              {/* Gender */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Gender
                </label>
                <select
                  name="Gender"
                  value={formData.Gender}
                  onChange={handleInputChange}
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  <option value="">Select Gender</option>
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                  <option value="Other">Other</option>
                  <option value="Prefer not to say">Prefer not to say</option>
                </select>
              </div>

              {/* Manager ID */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Manager ID
                </label>
                <input
                  type="text"
                  name="ManagerId"
                  value={formData.ManagerId}
                  onChange={handleInputChange}
                  className="w-full px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder="MGR001"
                />
              </div>
            </div>

            {/* Additional Attributes */}
            <div>
              <div className="flex justify-between items-center mb-2">
                <label className="block text-sm font-medium text-gray-700">
                  Additional Attributes
                </label>
                <button
                  type="button"
                  onClick={() => setAdditionalAttributes([...additionalAttributes, { Key: '', Value: '' }])}
                  className="text-sm text-indigo-600 hover:text-indigo-800 font-medium"
                >
                  + Add Attribute
                </button>
              </div>

              {additionalAttributes.length === 0 ? (
                <p className="text-sm text-gray-500 italic">No additional attributes. Click &quot;Add Attribute&quot; to add custom fields.</p>
              ) : (
                <div className="space-y-2">
                  {additionalAttributes.map((attr, index) => (
                    <div key={index} className="flex gap-2">
                      <input
                        type="text"
                        value={attr.Key}
                        onChange={(e) => {
                          const newAttrs = [...additionalAttributes];
                          newAttrs[index].Key = e.target.value;
                          setAdditionalAttributes(newAttrs);
                        }}
                        placeholder="Key (e.g., skills)"
                        className="flex-1 px-3 py-2 text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                      />
                      <input
                        type="text"
                        value={attr.Value}
                        onChange={(e) => {
                          const newAttrs = [...additionalAttributes];
                          newAttrs[index].Value = e.target.value;
                          setAdditionalAttributes(newAttrs);
                        }}
                        placeholder="Value (e.g., JavaScript, React)"
                        className="flex-1 px-3 py-2  text-black border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
                      />
                      <button
                        type="button"
                        onClick={() => {
                          const newAttrs = additionalAttributes.filter((_, i) => i !== index);
                          setAdditionalAttributes(newAttrs);
                        }}
                        className="px-3 py-2 text-red-600 hover:text-red-800 font-medium"
                      >
                        Remove
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="flex justify-end space-x-3 pt-4">
              <button
                type="button"
                onClick={resetForm}
                className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700"
              >
                {editingEmployee ? 'Update Employee' : 'Add Employee'}
              </button>
            </div>
          </form>
        )}
      </div>

      {/* Employees Table */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        {/* Search and Filter Bar */}
        <div className="p-6 border-b border-gray-200">
          <div className="flex gap-4">
            <input
              type="text"
              placeholder="Search by name, email, or employee ID..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="flex-1 border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
            <select
              value={filterStatus}
              onChange={(e) => setFilterStatus(e.target.value as 'all' | 'active' | 'inactive')}
              className="border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="all">All Status</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </select>
            <select
              value={filterDepartment}
              onChange={(e) => setFilterDepartment(e.target.value)}
              className="border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="all">All Departments</option>
              {Array.from(new Set(employees.map(e => e.Department).filter(Boolean))).map(dept => (
                <option key={dept} value={dept}>{dept}</option>
              ))}
            </select>
          </div>
        </div>

        {isLoading ? (
          <div className="flex justify-center items-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
          </div>
        ) : (() => {
          // Filter employees based on search and filters
          const filteredEmployees = employees.filter(employee => {
            const matchesSearch = searchTerm === '' ||
              employee.FullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
              employee.Email.toLowerCase().includes(searchTerm.toLowerCase()) ||
              employee.EmployeeId.toLowerCase().includes(searchTerm.toLowerCase());

            const matchesStatus = filterStatus === 'all' ||
              (filterStatus === 'active' && employee.IsActive) ||
              (filterStatus === 'inactive' && !employee.IsActive);

            const matchesDepartment = filterDepartment === 'all' || employee.Department === filterDepartment;

            return matchesSearch && matchesStatus && matchesDepartment;
          });

          return filteredEmployees.length === 0 ? (
            <div className="text-center py-12">
              <p className="text-gray-500">
                {searchTerm || filterStatus !== 'all' || filterDepartment !== 'all'
                  ? 'No employees match your filters'
                  : 'No employees found. Add your first employee to get started.'}
              </p>
            </div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Name
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Email
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Employee ID
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Department
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Designation
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Phone
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {filteredEmployees.map((employee) => (
                      <tr key={employee.Id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm font-medium text-gray-900">{employee.FullName}</div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm text-gray-900">{employee.Email}</div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm text-gray-900">{employee.EmployeeId}</div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm text-gray-900">{employee.Department || '-'}</div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm text-gray-900">{employee.Designation || '-'}</div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <div className="text-sm text-gray-900">{employee.Number || '-'}</div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                            employee.IsActive
                              ? 'bg-green-100 text-green-800'
                              : 'bg-red-100 text-red-800'
                          }`}>
                            {employee.IsActive ? 'Active' : 'Inactive'}
                          </span>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                          <button
                            onClick={() => handleEdit(employee)}
                            className="text-indigo-600 hover:text-indigo-900 mr-4"
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => handleDelete(employee.Id)}
                            className="text-red-600 hover:text-red-900"
                          >
                            Delete
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="px-6 py-4 border-t border-gray-200 bg-gray-50">
                <p className="text-sm text-gray-600">
                  Showing {filteredEmployees.length} of {employees.length} employees
                </p>
              </div>
            </>
          );
        })()}
      </div>
    </div>
  );
}
