'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import { subjectService, Subject, SubjectListResponse, EvaluatorRelationship } from '@/services/subjectService';
import RelationshipTagInput, { RelationshipTag } from '@/components/common/RelationshipTagInput';

interface ProjectSubjectsTabProps {
  projectSlug: string;
}

// Subject interface is now imported from service

export default function ProjectSubjectsTab({ projectSlug }: ProjectSubjectsTabProps) {
  const { token } = useAuth();
  const [subjects, setSubjects] = useState<SubjectListResponse[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingSubject, setEditingSubject] = useState<Subject | null>(null);
  const [formData, setFormData] = useState({
    FirstName: '',
    LastName: '',
    Email: '',
    EmployeeId: '',
    CompanyName: '',
    Gender: '',
    BusinessUnit: '',
    Grade: '',
    Designation: '',
    Tenure: '',
    Location: '',
    Metadata1: '',
    Metadata2: '',
    RelatedEmployeeIds: [] as string[],
    EvaluatorRelationships: [] as RelationshipTag[]
  });

  useEffect(() => {
    loadSubjects();
  }, [projectSlug]);

  const loadSubjects = async () => {
    if (!token) return;

    setIsLoading(true);
    try {
      const response = await subjectService.getSubjects(projectSlug, token);

      if (response.error) {
        setSubjects([]);
        toast.error(response.error);
      } else {
        setSubjects(response.data || []);
      }
    } catch (error) {
      console.error('Error loading subjects:', error);
      toast.error('Failed to load subjects');
      setSubjects([]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;

    setIsLoading(true);

    try {
      // Convert camelCase form data to PascalCase for API
      const submitData = {
        FirstName: formData.FirstName,
        LastName: formData.LastName,
        Email: formData.Email,
        EmployeeId: formData.EmployeeId,
        CompanyName: formData.CompanyName || undefined,
        Gender: formData.Gender || undefined,
        BusinessUnit: formData.BusinessUnit || undefined,
        Grade: formData.Grade || undefined,
        Designation: formData.Designation || undefined,
        Tenure: formData.Tenure ? parseInt(formData.Tenure) : undefined,
        Location: formData.Location || undefined,
        Metadata1: formData.Metadata1 || undefined,
        Metadata2: formData.Metadata2 || undefined,
        AssignedEvaluatorIds: [],
        RelatedEmployeeIds: formData.RelatedEmployeeIds.length > 0 ? formData.RelatedEmployeeIds : undefined,
        EvaluatorRelationships: formData.EvaluatorRelationships.length > 0
          ? formData.EvaluatorRelationships.map(rel => ({
              EvaluatorId: rel.employeeId,
              Relationship: rel.relationship
            }))
          : undefined
      };

      if (editingSubject) {
        // Update existing subject
        const response = await subjectService.updateSubject(projectSlug, editingSubject.Id, submitData, token);
        if (response.error) {
          toast.error(response.error);
          return;
        }
        toast.success('Subject updated successfully');
      } else {
        // Create new subject
        const response = await subjectService.createSubject(projectSlug, submitData, token);
        if (response.error) {
          toast.error(response.error);
          return;
        }
        toast.success('Subject created successfully');
      }

      resetForm();
      loadSubjects();
    } catch (error) {
      console.error('Error saving subject:', error);
      toast.error('Failed to save subject');
    } finally {
      setIsLoading(false);
    }
  };

  const handleEdit = (subject: SubjectListResponse) => {
    // Convert SubjectListResponse to Subject format for editing
    const subjectForEdit: Subject = {
      Id: subject.Id,
      FirstName: subject.FirstName,
      LastName: subject.LastName,
      FullName: subject.FullName,
      Email: subject.Email,
      EmployeeId: subject.EmployeeId,
      CompanyName: subject.CompanyName,
      Gender: '', // Not available in list response
      BusinessUnit: '', // Not available in list response
      Grade: '', // Not available in list response
      Designation: subject.Designation,
      Tenure: undefined, // Not available in list response
      Location: subject.Location,
      Metadata1: '', // Not available in list response
      Metadata2: '', // Not available in list response
      IsActive: subject.IsActive,
      CreatedAt: subject.CreatedAt,
      UpdatedAt: undefined,
      TenantId: subject.TenantId,
    
    };

    setEditingSubject(subjectForEdit);
    setFormData({
      FirstName: subject.FirstName || '',
      LastName: subject.LastName || '',
      Email: subject.Email || '',
      EmployeeId: subject.EmployeeId || '',
      CompanyName: subject.CompanyName || '',
      Gender: '', // Not available in list response
      BusinessUnit: '', // Not available in list response
      Grade: '', // Not available in list response
      Designation: subject.Designation || '',
      Tenure: '', // Not available in list response
      Location: subject.Location || '',
      Metadata1: '', // Not available in list response
      Metadata2: '', // Not available in list response
      RelatedEmployeeIds: [], // Not available in list response
      EvaluatorRelationships: [] // TODO: Load existing relationships from backend
    });
    setShowAddForm(true);
  };

  const handleDelete = async (subjectId: string) => {
    if (!confirm('Are you sure you want to delete this subject?')) return;
    if (!token) return;

    try {
      const response = await subjectService.deleteSubject(projectSlug, subjectId, token);
      if (response.error) {
        toast.error(response.error);
        return;
      }
      toast.success('Subject deleted successfully');
      loadSubjects();
    } catch (error) {
      console.error('Error deleting subject:', error);
      toast.error('Failed to delete subject');
    }
  };

  const resetForm = () => {
    setFormData({
      FirstName: '',
      LastName: '',
      Email: '',
      EmployeeId: '',
      CompanyName: '',
      Gender: '',
      BusinessUnit: '',
      Grade: '',
      Designation: '',
      Tenure: '',
      Location: '',
      Metadata1: '',
      Metadata2: '',
      RelatedEmployeeIds: [],
      EvaluatorRelationships: [],
    });
    setEditingSubject(null);
    setShowAddForm(false);
  };

  const handleBulkImport = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !token) return;

    const reader = new FileReader();
    reader.onload = async (event) => {
      try {
        const csvData = event.target?.result as string;
        const subjects = subjectService.parseCSV(csvData);

        if (subjects.length === 0) {
          toast.error('No valid subjects found in CSV file');
          return;
        }

        const response = await subjectService.bulkCreateSubjects(projectSlug, { Subjects: subjects }, token);
        if (response.error) {
          toast.error(response.error);
          return;
        }

        const result = response.data;
        if (result) {
          toast.success(`Bulk import completed: ${result.SuccessfullyCreated} created, ${result.Failed} errors`);
          if (result.Errors.length > 0) {
            console.error('Import errors:', result.Errors);
          }
        }
        loadSubjects();
      } catch (error) {
        console.error('Error during bulk import:', error);
        toast.error('Failed to import subjects');
      }
    };
    reader.readAsText(file);

    // Reset file input
    e.target.value = '';
  };

  const downloadTemplate = () => {
    const csvContent = subjectService.generateCSVTemplate();
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'subjects_template.csv';
    a.click();
    window.URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white shadow rounded-lg p-6">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text-2xl font-bold text-gray-900">Subjects</h2>
            <p className="text-gray-600">Manage participants who will take surveys</p>
          </div>
          <div className="flex space-x-3">
            <button
              onClick={downloadTemplate}
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              Download Template
            </button>
            <label className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium cursor-pointer">
              Bulk Import CSV
              <input
                type="file"
                accept=".csv,.xlsx,.xls"
                onChange={handleBulkImport}
                className="hidden"
              />
            </label>
            <button
              onClick={() => setShowAddForm(true)}
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              Add Subject
            </button>
            <button
              onClick={() => window.open(`/projects/${projectSlug}/subject-evaluator-connections`, '_blank')}
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              Manage Connections
            </button>
          </div>
        </div>

        {/* Add/Edit Form */}
        {showAddForm && (
          <div className="border-t pt-6 mt-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">
              {editingSubject ? 'Edit Subject' : 'Add New Subject'}
            </h3>
            <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-2 gap-4 bg-white p-6 rounded-lg">
              <div>
                <label className="block text-sm font-medium text-gray-900">First Name</label>
                <input
                  type="text"
                  name="FirstName"
                  value={formData.FirstName}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Last Name</label>
                <input
                  type="text"
                  name="LastName"
                  value={formData.LastName}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Email</label>
                <input
                  type="email"
                  name="Email"
                  value={formData.Email}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Employee ID</label>
                <input
                  type="text"
                  name="EmployeeId"
                  value={formData.EmployeeId}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                  required
                  maxLength={50}
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Company Name</label>
                <input
                  type="text"
                  name="CompanyName"
                  value={formData.CompanyName}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Gender</label>
                <select
                  name="gender"
                  value={formData.Gender}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="">Select Gender</option>
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                  <option value="Other">Other</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Business Unit</label>
                <input
                  type="text"
                  name="BusinessUnit"
                  value={formData.BusinessUnit}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Grade</label>
                <input
                  type="text"
                  name="Grade"
                  value={formData.Grade}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Designation</label>
                <input
                  type="text"
                  name="Designation"
                  value={formData.Designation}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Tenure (Years)</label>
                <input
                  type="number"
                  name="Tenure"
                  value={formData.Tenure}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                  min="0"
                  max="100"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Location</label>
                <input
                  type="text"
                  name="Location"
                  value={formData.Location}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Metadata 1</label>
                <input
                  type="text"
                  name="Metadata1"
                  value={formData.Metadata1}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-900">Metadata 2</label>
                <input
                  type="text"
                  name="Metadata2"
                  value={formData.Metadata2}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              {/* Enhanced Evaluator Relationships Section */}
              <div className="md:col-span-2">
                <RelationshipTagInput
                  label="Evaluator Relationships"
                  placeholder="Enter evaluator Employee ID(s), comma-separated..."
                  tags={formData.EvaluatorRelationships}
                  onTagsChange={(tags) => setFormData(prev => ({ ...prev, EvaluatorRelationships: tags }))}
                  onValidate={async (employeeIds) => {
                    if (!token) return [];
                    const idsArray = Array.isArray(employeeIds) ? employeeIds : [employeeIds];
                    const validIds = await subjectService.validateEvaluatorIds(projectSlug, idsArray, token);
                    console.log('Valid IDs:', validIds);
                    return validIds;
                  }}
                  relationshipOptions={['manager', 'peer', 'subordinate', 'lead', 'trainee', 'mentor', 'other']}
                  className="mb-4"
                />
              </div>

              <div className="md:col-span-2 flex justify-end space-x-3">
                <button
                  type="button"
                  onClick={resetForm}
                  className="bg-gray-300 hover:bg-gray-400 text-gray-700 px-4 py-2 rounded-md"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isLoading}
                  className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md"
                >
                  {isLoading ? 'Saving...' : editingSubject ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        )}
      </div>

      {/* Subjects List */}
      <div className="bg-white shadow rounded-lg p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Subjects List</h3>
        {isLoading ? (
          <div className="text-center py-4">Loading subjects...</div>
        ) : subjects.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            No subjects found. Add your first subject above.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Email</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Employee ID</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Company</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Designation</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {subjects.map((subject) => (
                  <tr key={subject.Id}>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">
                        {subject.FirstName} {subject.LastName}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {subject.Email}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {subject.EmployeeId}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {subject.CompanyName || '-'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {subject.Designation || '-'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button
                        onClick={() => handleEdit(subject)}
                        className="text-blue-600 hover:text-blue-900 mr-3"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(subject.Id)}
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
        )}
      </div>
    </div>
  );
}
