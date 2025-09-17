'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import { subjectService, Subject, CreateSubjectRequest } from '@/services/subjectService';

interface ProjectSubjectsTabProps {
  projectSlug: string;
}

// Subject interface is now imported from service

export default function ProjectSubjectsTab({ projectSlug }: ProjectSubjectsTabProps) {
  const { token } = useAuth();
  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingSubject, setEditingSubject] = useState<Subject | null>(null);
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    department: '',
    position: ''
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

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;

    setIsLoading(true);

    try {
      if (editingSubject) {
        // Update existing subject
        const response = await subjectService.updateSubject(projectSlug, editingSubject.id, formData, token);
        if (response.error) {
          toast.error(response.error);
          return;
        }
        toast.success('Subject updated successfully');
      } else {
        // Create new subject
        const response = await subjectService.createSubject(projectSlug, formData, token);
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

  const handleEdit = (subject: Subject) => {
    setEditingSubject(subject);
    setFormData({
      firstName: subject.firstName,
      lastName: subject.lastName,
      email: subject.email,
      phoneNumber: subject.phoneNumber,
      department: subject.department,
      position: subject.position
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
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      department: '',
      position: ''
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

        const response = await subjectService.bulkCreateSubjects(projectSlug, { subjects }, token);
        if (response.error) {
          toast.error(response.error);
          return;
        }

        const result = response.data;
        if (result) {
          toast.success(`Bulk import completed: ${result.successCount} created, ${result.errorCount} errors`);
          if (result.errors.length > 0) {
            console.error('Import errors:', result.errors);
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
              className="bg-gray-500 hover:bg-gray-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              Download Template
            </button>
            <label className="bg-green-500 hover:bg-green-700 text-white px-4 py-2 rounded-md text-sm font-medium cursor-pointer">
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
              className="bg-purple-500 hover:bg-purple-700 text-white px-4 py-2 rounded-md text-sm font-medium"
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
            <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">First Name</label>
                <input
                  type="text"
                  name="firstName"
                  value={formData.firstName}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Last Name</label>
                <input
                  type="text"
                  name="lastName"
                  value={formData.lastName}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Email</label>
                <input
                  type="email"
                  name="email"
                  value={formData.email}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Phone Number</label>
                <input
                  type="tel"
                  name="phoneNumber"
                  value={formData.phoneNumber}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Department</label>
                <input
                  type="text"
                  name="department"
                  value={formData.department}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Position</label>
                <input
                  type="text"
                  name="position"
                  value={formData.position}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
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
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Department</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Position</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {subjects.map((subject) => (
                  <tr key={subject.id}>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">
                        {subject.firstName} {subject.lastName}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {subject.email}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {subject.department}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {subject.position}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button
                        onClick={() => handleEdit(subject)}
                        className="text-blue-600 hover:text-blue-900 mr-3"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(subject.id)}
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
