'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import { evaluatorService, Evaluator, CreateEvaluatorRequest } from '@/services/evaluatorService';

interface ProjectEvaluatorsTabProps {
  projectSlug: string;
}

// Evaluator interface is now imported from service

export default function ProjectEvaluatorsTab({ projectSlug }: ProjectEvaluatorsTabProps) {
  const { token } = useAuth();
  const [evaluators, setEvaluators] = useState<Evaluator[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingEvaluator, setEditingEvaluator] = useState<Evaluator | null>(null);
  const [formData, setFormData] = useState({
    evaluatorFirstName: '',
    evaluatorLastName: '',
    evaluatorEmail: '',
    evaluatorPhoneNumber: '',
    evaluatorDepartment: '',
    evaluatorPosition: ''
  });

  useEffect(() => {
    loadEvaluators();
  }, [projectSlug]);

  const loadEvaluators = async () => {
    if (!token) return;

    setIsLoading(true);
    try {
      const response = await evaluatorService.getEvaluators(projectSlug, token);
      if (response.error) {
        setEvaluators([]);
        toast.error(response.error);
      } else {
        setEvaluators(response.data || []);
      }
    } catch (error) {
      console.error('Error loading evaluators:', error);
      toast.error('Failed to load evaluators');
      setEvaluators([]);
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
      if (editingEvaluator) {
        // Update existing evaluator
        const response = await evaluatorService.updateEvaluator(projectSlug, editingEvaluator.id, formData, token);
        if (response.error) {
          toast.error(response.error);
          return;
        }
        toast.success('Evaluator updated successfully');
      } else {
        // Create new evaluator
        const response = await evaluatorService.createEvaluator(projectSlug, formData, token);
        if (response.error) {
          toast.error(response.error);
          return;
        }
        toast.success('Evaluator created successfully');
      }

      resetForm();
      loadEvaluators();
    } catch (error) {
      console.error('Error saving evaluator:', error);
      toast.error('Failed to save evaluator');
    } finally {
      setIsLoading(false);
    }
  };

  const handleEdit = (evaluator: Evaluator) => {
    setEditingEvaluator(evaluator);
    setFormData({
      evaluatorFirstName: evaluator.evaluatorFirstName,
      evaluatorLastName: evaluator.evaluatorLastName,
      evaluatorEmail: evaluator.evaluatorEmail,
      evaluatorPhoneNumber: evaluator.evaluatorPhoneNumber,
      evaluatorDepartment: evaluator.evaluatorDepartment,
      evaluatorPosition: evaluator.evaluatorPosition
    });
    setShowAddForm(true);
  };

  const handleDelete = async (evaluatorId: string) => {
    if (!confirm('Are you sure you want to delete this evaluator?')) return;
    if (!token) return;

    try {
      const response = await evaluatorService.deleteEvaluator(projectSlug, evaluatorId, token);
      if (response.error) {
        toast.error(response.error);
        return;
      }
      toast.success('Evaluator deleted successfully');
      loadEvaluators();
    } catch (error) {
      console.error('Error deleting evaluator:', error);
      toast.error('Failed to delete evaluator');
    }
  };

  const resetForm = () => {
    setFormData({
      evaluatorFirstName: '',
      evaluatorLastName: '',
      evaluatorEmail: '',
      evaluatorPhoneNumber: '',
      evaluatorDepartment: '',
      evaluatorPosition: ''
    });
    setEditingEvaluator(null);
    setShowAddForm(false);
  };

  const handleBulkImport = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !token) return;

    const reader = new FileReader();
    reader.onload = async (event) => {
      try {
        const csvData = event.target?.result as string;
        const evaluators = evaluatorService.parseCSV(csvData);

        if (evaluators.length === 0) {
          toast.error('No valid evaluators found in CSV file');
          return;
        }

        const response = await evaluatorService.bulkCreateEvaluators(projectSlug, { evaluators }, token);
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
        loadEvaluators();
      } catch (error) {
        console.error('Error during bulk import:', error);
        toast.error('Failed to import evaluators');
      }
    };
    reader.readAsText(file);

    // Reset file input
    e.target.value = '';
  };

  const downloadTemplate = () => {
    const csvContent = evaluatorService.generateCSVTemplate();
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'evaluators_template.csv';
    a.click();
    window.URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white shadow rounded-lg p-6">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text-2xl font-bold text-gray-900">Evaluators</h2>
            <p className="text-gray-600">Manage evaluators who will assess participants</p>
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
              Add Evaluator
            </button>
          </div>
        </div>

        {/* Add/Edit Form */}
        {showAddForm && (
          <div className="border-t pt-6 mt-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">
              {editingEvaluator ? 'Edit Evaluator' : 'Add New Evaluator'}
            </h3>
            <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700">First Name</label>
                <input
                  type="text"
                  name="evaluatorFirstName"
                  value={formData.evaluatorFirstName}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Last Name</label>
                <input
                  type="text"
                  name="evaluatorLastName"
                  value={formData.evaluatorLastName}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Email</label>
                <input
                  type="email"
                  name="evaluatorEmail"
                  value={formData.evaluatorEmail}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Phone Number</label>
                <input
                  type="tel"
                  name="evaluatorPhoneNumber"
                  value={formData.evaluatorPhoneNumber}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Department</label>
                <input
                  type="text"
                  name="evaluatorDepartment"
                  value={formData.evaluatorDepartment}
                  onChange={handleInputChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md px-3 py-2"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Position</label>
                <input
                  type="text"
                  name="evaluatorPosition"
                  value={formData.evaluatorPosition}
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
                  {isLoading ? 'Saving...' : editingEvaluator ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        )}
      </div>

      {/* Evaluators List */}
      <div className="bg-white shadow rounded-lg p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Evaluators List</h3>
        {isLoading ? (
          <div className="text-center py-4">Loading evaluators...</div>
        ) : evaluators.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            No evaluators found. Add your first evaluator above.
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
                {evaluators.map((evaluator) => (
                  <tr key={evaluator.id}>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">
                        {evaluator.evaluatorFirstName} {evaluator.evaluatorLastName}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {evaluator.evaluatorEmail}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {evaluator.evaluatorDepartment}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {evaluator.evaluatorPosition}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button
                        onClick={() => handleEdit(evaluator)}
                        className="text-blue-600 hover:text-blue-900 mr-3"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(evaluator.id)}
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
