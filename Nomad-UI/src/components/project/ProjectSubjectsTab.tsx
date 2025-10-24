/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import { subjectService, SubjectListResponse, EvaluatorRelationship } from '@/services/subjectService';
import { evaluatorService, EvaluatorListResponse } from '@/services/evaluatorService';
import { employeeService, EmployeeListResponse } from '@/services/employeeService';
import ManageRelationshipsModal from '@/components/modals/ManageRelationshipsModal';

interface ProjectSubjectsTabProps {
  projectSlug: string;
}

export default function ProjectSubjectsTab({ projectSlug }: ProjectSubjectsTabProps) {
  const { token } = useAuth();
  const [subjects, setSubjects] = useState<SubjectListResponse[]>([]);
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
  const [selectedSubject, setSelectedSubject] = useState<{ id: string; name: string } | null>(null);

  useEffect(() => {
    loadSubjects();
    loadEmployees();
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
    // Filter out employees who are already subjects
    const subjectEmployeeIds = subjects.map(s => s.EmployeeIdString);
    return employees.filter(emp => !subjectEmployeeIds.includes(emp.EmployeeId));
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
      const subjectsToCreate = selectedEmployeeIds.map(employeeId => ({
        EmployeeId: employeeId,
        EvaluatorRelationships: [] as EvaluatorRelationship[]
      }));

      const response = await subjectService.bulkCreateSubjects(
        projectSlug,
        { Subjects: subjectsToCreate },
        token
      );

      if (response.error) {
        toast.error(response.error);
      } else if (response.data) {
        toast.success(`Successfully created ${response.data.SuccessfullyCreated} subject(s)`);
        if (response.data.Errors && response.data.Errors.length > 0) {
          response.data.Errors.forEach(err => toast.error(err));
        }
        setSelectedEmployeeIds([]);
        setShowAddForm(false);
        loadSubjects();
      }
    } catch (error) {
      console.error('Error creating subjects:', error);
      toast.error('Failed to create subjects');
    } finally {
      setIsLoading(false);
    }
  };

  const handleBulkImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !token) return;

    setIsImporting(true);
    try {
      const text = await file.text();
      const parseResult = subjectService.parseCSV(text);

      if (parseResult.errors.length > 0) {
        parseResult.errors.forEach(err => toast.error(err));
        if (parseResult.subjects.length === 0) {
          setIsImporting(false);
          return;
        }
      }

      const response = await subjectService.bulkCreateSubjects(
        projectSlug,
        { Subjects: parseResult.subjects },
        token
      );

      if (response.error) {
        toast.error(response.error);
      } else if (response.data) {
        toast.success(`Successfully imported ${response.data.SuccessfullyCreated} subject(s)`);
        if (response.data.Errors && response.data.Errors.length > 0) {
          response.data.Errors.forEach(err => toast.error(err));
        }
        loadSubjects();
      }
    } catch (error) {
      console.error('Error importing subjects:', error);
      toast.error('Failed to import subjects');
    } finally {
      setIsImporting(false);
      e.target.value = '';
    }
  };

  const downloadTemplate = () => {
    const csvContent = `EmployeeId,EvaluatorRelationships
EMP001,"[{""EvaluatorEmployeeId"":""EMP002"",""Relationship"":""Manager""}]"
EMP003,"[{""EvaluatorEmployeeId"":""EMP004"",""Relationship"":""Peer""}]"`;
    
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'subjects_template.csv';
    a.click();
    window.URL.revokeObjectURL(url);
  };

  const handleDelete = async (subjectId: string) => {
    if (!confirm('Are you sure you want to delete this subject?')) return;
    if (!token) return;

    try {
      const response = await subjectService.deleteSubject(projectSlug, subjectId, token);
      if (response.error) {
        toast.error(response.error);
      } else {
        toast.success('Subject deleted successfully');
        loadSubjects();
      }
    } catch (error) {
      console.error('Error deleting subject:', error);
      toast.error('Failed to delete subject');
    }
  };

  // Filter subjects based on search and status
  const filteredSubjects = subjects.filter(subject => {
    const matchesSearch = searchTerm === '' || 
      subject.FullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      subject.Email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      subject.EmployeeIdString.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesStatus = filterStatus === 'all' || 
      (filterStatus === 'active' && subject.IsActive) ||
      (filterStatus === 'inactive' && !subject.IsActive);
    
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
          <h2 className="text-2xl font-bold text-gray-900">Subjects</h2>
          <div className="flex gap-2">
            <button
              onClick={downloadTemplate}
              className="bg-blue-500 hover:bg-gray-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              Download CSV Template
            </button>
            <label className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium cursor-pointer">
              Bulk Import CSV
              <input
                type="file"
                accept=".csv"
                onChange={handleBulkImport}
                className="hidden"
                disabled={isImporting}
              />
            </label>
            <button
              onClick={() => setShowAddForm(!showAddForm)}
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              {showAddForm ? 'Cancel' : 'Add Subjects'}
            </button>
            <button
              onClick={() => window.open(`/projects/${projectSlug}/subject-evaluator-connections`, '_blank')}
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              Manage Connections
            </button>
          </div>
        </div>

        {/* Add Form - Employee Multi-Select */}
        {showAddForm && (
          <div className="border-t pt-6 mt-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">
              Select Employees to Add as Subjects
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
              
              <div className="mb-4 flex items-center gap-4">
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
              </div>

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
                        onChange={() => {}}
                        className="mr-3 cursor-pointer"
                        onClick={(e) => e.stopPropagation()}
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
                  {isLoading ? 'Creating...' : `Create ${selectedEmployeeIds.length} Subject(s)`}
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Search and Filter Bar */}
        <div className="mb-6 flex gap-4">
          <input
            type="text"
            placeholder="Search subjects by name, email, or employee ID..."
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

        {/* Subjects List */}
        {isLoading ? (
          <div className="text-center py-8">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
            <p className="mt-2 text-gray-600">Loading subjects...</p>
          </div>
        ) : filteredSubjects.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            {searchTerm || filterStatus !== 'all' ? 'No subjects match your filters' : 'No subjects found. Add some subjects to get started.'}
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
                    Evaluators
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
                {filteredSubjects.map((subject) => (
                  <tr key={subject.Id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {subject.EmployeeIdString}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {subject.FullName}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                      {subject.Email}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                      {subject.Designation || '-'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                      {subject.EvaluatorCount || 0}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                        subject.IsActive
                          ? 'bg-green-100 text-green-800'
                          : 'bg-red-100 text-red-800'
                      }`}>
                        {subject.IsActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button
                        onClick={() => {
                          setSelectedSubject({ id: subject.Id, name: subject.FullName });
                          setShowRelationshipsModal(true);
                        }}
                        className="text-indigo-600 hover:text-indigo-900 mr-4"
                      >
                        Manage
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

        {/* Summary */}
        <div className="mt-4 text-sm text-gray-600">
          Showing {filteredSubjects.length} of {subjects.length} subjects
        </div>
      </div>

      {/* Manage Relationships Modal */}
      <ManageRelationshipsModal
        isOpen={showRelationshipsModal}
        onClose={() => {
          setShowRelationshipsModal(false);
          setSelectedSubject(null);
        }}
        entityType="subject"
        entityId={selectedSubject?.id || null}
        entityName={selectedSubject?.name || ''}
        projectSlug={projectSlug}
        onRelationshipsUpdated={loadSubjects}
      />
    </div>
  );
}

