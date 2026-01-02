/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import { evaluatorService, EvaluatorListResponse, SubjectRelationship } from '@/services/evaluatorService';
import { subjectService, SubjectListResponse } from '@/services/subjectService';
import { employeeService, EmployeeListResponse } from '@/services/employeeService';
import ManageRelationshipsModal from '@/components/modals/ManageRelationshipsModal';

interface ProjectEvaluatorsTabProps {
  projectSlug: string;
}

export default function ProjectEvaluatorsTab({ projectSlug }: ProjectEvaluatorsTabProps) {
  const { token } = useAuth();
  const [evaluators, setEvaluators] = useState<EvaluatorListResponse[]>([]);
  const [employees, setEmployees] = useState<EmployeeListResponse[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showAddForm, setShowAddForm] = useState(false);

  // Multi-select state
  const [selectedEmployeeIds, setSelectedEmployeeIds] = useState<string[]>([]);

  // Search and filter state
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<'all' | 'active' | 'inactive'>('all');

  // CSV import state
  const [isImporting, setIsImporting] = useState(false);

  // Modal state
  const [showRelationshipsModal, setShowRelationshipsModal] = useState(false);
  const [selectedEvaluator, setSelectedEvaluator] = useState<{ id: string; name: string } | null>(null);

  useEffect(() => {
    loadEvaluators();
    loadEmployees();
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

  const loadEmployees = async () => {
    if (!token) return;

    try {
      const response = await employeeService.getEmployees(projectSlug, token);
      if (response.error) {
        console.error('Error loading employees:', response.error);
      } else if (response.data) {
        setEmployees(response.data);
      }
    } catch (error) {
      console.error('Error loading employees:', error);
    }
  };

  const handleEmployeeSelect = (employeeId: string) => {
    setSelectedEmployeeIds(prev => {
      if (prev.includes(employeeId)) {
        return prev.filter(id => id !== employeeId);
      } else {
        return [...prev, employeeId];
      }
    });
  };

  const handleSelectAll = () => {
    const availableEmployees = getAvailableEmployees();
    if (selectedEmployeeIds.length === availableEmployees.length) {
      setSelectedEmployeeIds([]);
    } else {
      setSelectedEmployeeIds(availableEmployees.map(emp => emp.EmployeeId));
    }
  };

  const getAvailableEmployees = () => {
    // Filter out employees who are already evaluators
    const evaluatorEmployeeIds = evaluators.map(e => e.EmployeeIdString);
    return employees.filter(emp => !evaluatorEmployeeIds.includes(emp.EmployeeId));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (selectedEmployeeIds.length === 0) {
      toast.error('Please select at least one employee');
      return;
    }

    if (!token) {
      toast.error('Authentication required');
      return;
    }

    setIsLoading(true);
    try {
      const evaluatorsToCreate = selectedEmployeeIds.map(employeeId => ({
        EmployeeId: employeeId,
        SubjectRelationships: [] as SubjectRelationship[]
      }));

      const response = await evaluatorService.bulkCreateEvaluators(
        projectSlug,
        { Evaluators: evaluatorsToCreate },
        token
      );

      if (response.error) {
        toast.error(response.error);
      } else if (response.data) {
        toast.success(`Successfully created ${response.data.SuccessfullyCreated} evaluator(s)`);
        if (response.data.Errors && response.data.Errors.length > 0) {
          response.data.Errors.forEach(err => toast.error(err));
        }
        setSelectedEmployeeIds([]);
        setShowAddForm(false);
        loadEvaluators();
      }
    } catch (error) {
      console.error('Error creating evaluators:', error);
      toast.error('Failed to create evaluators');
    } finally {
      setIsLoading(false);
    }
  };

  const handleBulkImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    console.log('🔵 [EVALUATORS] handleBulkImport TRIGGERED');

    const file = e.target.files?.[0];
    console.log('📁 [EVALUATORS] File object:', file);
    console.log('🔑 [EVALUATORS] Token exists:', !!token);
    console.log('📍 [EVALUATORS] Project slug:', projectSlug);

    if (!file) {
      console.log('❌ [EVALUATORS] No file selected');
      return;
    }

    if (!token) {
      console.log('❌ [EVALUATORS] No token available');
      toast.error('Authentication token not found. Please log in again.');
      return;
    }

    setIsImporting(true);
    try {
      console.log('📖 [EVALUATORS] Reading file content...');
      const text = await file.text();
      console.log('📄 [EVALUATORS] File content length:', text.length);

      console.log('🔍 [EVALUATORS] Parsing CSV...');
      const parseResult = evaluatorService.parseCSV(text);
      console.log('✅ [EVALUATORS] Parse result:', {
        evaluatorsCount: parseResult.evaluators.length,
        errorsCount: parseResult.errors.length
      });

      if (parseResult.errors.length > 0) {
        parseResult.errors.forEach(err => toast.error(err));
        if (parseResult.evaluators.length === 0) {
          setIsImporting(false);
          return;
        }
      }

      console.log('🚀 [EVALUATORS] Calling bulkCreateEvaluators API...');
      const response = await evaluatorService.bulkCreateEvaluators(
        projectSlug,
        { Evaluators: parseResult.evaluators },
        token
      );

      console.log('📥 [EVALUATORS] API response:', response);

      if (response.error) {
        toast.error(response.error);
      } else if (response.data) {
        toast.success(`Successfully imported ${response.data.SuccessfullyCreated} evaluator(s)`);
        if (response.data.Errors && response.data.Errors.length > 0) {
          response.data.Errors.forEach(err => toast.error(err));
        }
        loadEvaluators();
      }
    } catch (error) {
      console.error('❌ [EVALUATORS] Error importing evaluators:', error);
      toast.error('Failed to import evaluators');
    } finally {
      setIsImporting(false);
      e.target.value = '';
    }
  };

  const downloadTemplate = () => {
    // New flattened CSV format - simpler and more user-friendly
    const csvContent = `EmployeeId,SubjectEmployeeId,Relationship
EMP001,EMP002,Manager
EMP001,EMP003,Peer
EMP002,EMP001,Direct Report
EMP002,EMP004,Peer`;

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'evaluators_template.csv';
    a.click();
    window.URL.revokeObjectURL(url);
  };

  const handleDelete = async (evaluatorId: string) => {
    if (!confirm('Are you sure you want to delete this evaluator?')) return;
    if (!token) return;

    try {
      const response = await evaluatorService.deleteEvaluator(projectSlug, evaluatorId, token);
      if (response.error) {
        toast.error(response.error);
      } else {
        toast.success('Evaluator deleted successfully');
        loadEvaluators();
      }
    } catch (error) {
      console.error('Error deleting evaluator:', error);
      toast.error('Failed to delete evaluator');
    }
  };

  // Filter evaluators based on search and status
  const filteredEvaluators = evaluators.filter(evaluator => {
    const matchesSearch = searchTerm === '' ||
      evaluator.FullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      evaluator.Email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      evaluator.EmployeeIdString.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesStatus = filterStatus === 'all' ||
      (filterStatus === 'active' && evaluator.IsActive) ||
      (filterStatus === 'inactive' && !evaluator.IsActive);

    return matchesSearch && matchesStatus;
  });

  // Filter available employees for multi-select
  const filteredAvailableEmployees = getAvailableEmployees().filter(emp =>
    searchTerm === '' ||
    emp.FullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    emp.Email.toLowerCase().includes(searchTerm.toLowerCase()) ||
    emp.EmployeeId.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="space-y-6">
      <div className="bg-white shadow-md rounded-lg p-6">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-900">Evaluators</h2>
          <div className="flex gap-2">
            <button
              onClick={downloadTemplate}
              className="bg-blue-500 hover:bg-gray-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              Download CSV Template
            </button>
            <label
              htmlFor="evaluator-csv-upload"
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium cursor-pointer inline-block"
              style={{ pointerEvents: 'auto' }}
            >
              {isImporting ? 'Importing...' : 'Bulk Import CSV'}
              <input
                id="evaluator-csv-upload"
                type="file"
                accept=".csv"
                onChange={handleBulkImport}
                className="hidden"
                disabled={isImporting}
                style={{ display: 'none' }}
              />
            </label>
            <button
              onClick={() => setShowAddForm(!showAddForm)}
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              {showAddForm ? 'Cancel' : 'Add Evaluators'}
            </button>
            {/* <button
              onClick={() => window.open(`/projects/${projectSlug}/subject-evaluator-connections`, '_blank')}
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              Manage Connections
            </button> */}
          </div>
        </div>

        {/* Add Form - Employee Multi-Select */}
        {showAddForm && (
          <div className="border-t pt-6 mt-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">
              Select Employees to Add as Evaluators
            </h3>
            <form onSubmit={handleSubmit} className="bg-gray-50 p-6 rounded-lg">
              <div className="mb-4">
                <input
                  type="text"
                  placeholder="Search employees..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-full border border-gray-300 rounded-md px-3 py-2 text-gray-900 bg-white"
                />
              </div>

              {/* <div className="mb-4 flex items-center gap-4">
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={selectedEmployeeIds.length === filteredAvailableEmployees.length && filteredAvailableEmployees.length > 0}
                    onChange={handleSelectAll}
                    className="mr-2"
                  />
                  <span className="text-sm font-medium text-gray-700">Select All ({filteredAvailableEmployees.length})</span>
                </label>
                <span className="text-sm text-gray-600">
                  {selectedEmployeeIds.length} selected
                </span>
              </div> */}

              <div className="max-h-96 overflow-y-auto border border-gray-300 rounded-md bg-white">
                {filteredAvailableEmployees.length === 0 ? (
                  <div className="p-4 text-center text-gray-500">
                    No available employees found
                  </div>
                ) : (
                  filteredAvailableEmployees.map(employee => (
                    <div
                      key={employee.Id}
                      className="flex items-center p-3 hover:bg-gray-50 border-b border-gray-200 cursor-pointer"
                      onClick={() => handleEmployeeSelect(employee.EmployeeId)}

                    >
                      <input
                        type="checkbox"
                        checked={selectedEmployeeIds.includes(employee.EmployeeId)}
                        onChange={() => handleEmployeeSelect(employee.EmployeeId)}
                        className="mr-3 relative z-10 cursor-pointer"
                        // onClick={(e) => e.stopPropagation()}
                        onClick={(e) => e.stopPropagation()}

                      // onClick={() => handleEmployeeSelect(employee.EmployeeId)}

                      />
                      <div className="flex-1">
                        <div className="font-medium text-gray-900">
                          {employee.FullName} ({employee.EmployeeId})
                        </div>
                        <div className="text-sm text-gray-600">
                          {employee.Email} {employee.Designation && `• ${employee.Designation}`}
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>

              <div className="mt-4 flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setShowAddForm(false);
                    setSelectedEmployeeIds([]);
                    setSearchTerm('');
                  }}
                  className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isLoading || selectedEmployeeIds.length === 0}
                  className="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-700 disabled:bg-gray-300"
                >
                  {isLoading ? 'Creating...' : `Create ${selectedEmployeeIds.length} Evaluator(s)`}
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Search and Filter Bar */}
        <div className="mb-6 flex gap-4">
          <input
            type="text"
            placeholder="Search evaluators by name, email, or employee ID..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="flex-1 border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white"
          />
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value as 'all' | 'active' | 'inactive')}
            className="border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white"
          >
            <option value="all">All Status</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>
        </div>

        {/* Evaluators List */}
        {isLoading ? (
          <div className="text-center py-8">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
            <p className="mt-2 text-gray-600">Loading evaluators...</p>
          </div>
        ) : filteredEvaluators.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            {searchTerm || filterStatus !== 'all' ? 'No evaluators match your filters' : 'No evaluators found. Add some evaluators to get started.'}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Employee ID
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Name
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Email
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Designation
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Subjects
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {filteredEvaluators.map((evaluator) => (
                  <tr key={evaluator.Id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {evaluator.EmployeeIdString}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {evaluator.FullName}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                      {evaluator.Email}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                      {evaluator.Designation || '-'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                      {evaluator.SubjectCount || 0}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${evaluator.IsActive
                        ? 'bg-green-100 text-green-800'
                        : 'bg-red-100 text-red-800'
                        }`}>
                        {evaluator.IsActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button
                        onClick={() => {
                          setSelectedEvaluator({ id: evaluator.Id, name: evaluator.FullName });
                          setShowRelationshipsModal(true);
                        }}
                        className="text-indigo-600 hover:text-indigo-900 mr-4"
                      >
                        Manage
                      </button>
                      <button
                        onClick={() => handleDelete(evaluator.Id)}
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

        {/* Summary */}
        <div className="mt-4 text-sm text-gray-600">
          Showing {filteredEvaluators.length} of {evaluators.length} evaluators
        </div>
      </div>

      {/* Manage Relationships Modal */}
      <ManageRelationshipsModal
        isOpen={showRelationshipsModal}
        onClose={() => {
          setShowRelationshipsModal(false);
          setSelectedEvaluator(null);
        }}
        entityType="evaluator"
        entityId={selectedEvaluator?.id || null}
        entityName={selectedEvaluator?.name || ''}
        projectSlug={projectSlug}
        onRelationshipsUpdated={loadEvaluators}
      />
    </div >
  );
}

