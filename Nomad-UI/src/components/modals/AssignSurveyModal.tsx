'use client';

import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import styles from './AssignSurveyModal.module.css';

interface AssignedRelationship {
  Id: string;
  SubjectEvaluatorId: string;
  Relationship: string;
  SubjectId: string;
  SubjectFullName: string;
  SubjectEmail: string;
  SubjectEmployeeIdString: string;
  SubjectDesignation?: string;
  EvaluatorId: string;
  EvaluatorFullName: string;
  EvaluatorEmail: string;
  EvaluatorEmployeeIdString: string;
  EvaluatorDesignation?: string;
  IsActive: boolean;
  CreatedAt: string;
}

interface AvailableRelationship {
  SubjectEvaluatorId: string;
  Relationship: string;
  SubjectId: string;
  SubjectFullName: string;
  SubjectEmail: string;
  SubjectEmployeeIdString: string;
  SubjectDesignation?: string;
  EvaluatorId: string;
  EvaluatorFullName: string;
  EvaluatorEmail: string;
  EvaluatorEmployeeIdString: string;
  EvaluatorDesignation?: string;
  IsActive: boolean;
}

interface AssignSurveyModalProps {
  isOpen: boolean;
  onClose: () => void;
  surveyId: string;
  surveyTitle: string;
  isSelfEvaluation: boolean;
  projectSlug: string;
  onAssignmentUpdated: () => void;
}

export default function AssignSurveyModal({
  isOpen,
  onClose,
  surveyId,
  surveyTitle,
  isSelfEvaluation,
  projectSlug,
  onAssignmentUpdated
}: AssignSurveyModalProps) {
  const { token } = useAuth();
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isCsvUploading, setIsCsvUploading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedDesignation, setSelectedDesignation] = useState<string>('');
  const [assignedRelationships, setAssignedRelationships] = useState<AssignedRelationship[]>([]);
  const [availableRelationships, setAvailableRelationships] = useState<AvailableRelationship[]>([]);
  const [selectedAssigned, setSelectedAssigned] = useState<Set<string>>(new Set());
  const [selectedAvailable, setSelectedAvailable] = useState<Set<string>>(new Set());
  const [activeTab, setActiveTab] = useState<'assigned' | 'available'>('available');

  useEffect(() => {
    if (isOpen) {
      loadRelationships();
    }
  }, [isOpen, surveyId]);

  const loadRelationships = async () => {
    if (!token) return;

    setIsLoading(true);
    try {
      // Load assigned relationships
      const assignedResponse = await fetch(
        `/api/${projectSlug}/surveys/${surveyId}/assigned-relationships`,
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          }
        }
      );

      if (assignedResponse.ok) {
        const assignedData = await assignedResponse.json();
        setAssignedRelationships(assignedData);
        // Pre-select all assigned relationships
        setSelectedAssigned(new Set(assignedData.map((r: AssignedRelationship) => r.SubjectEvaluatorId)));
      }

      // Load available relationships
      const availableResponse = await fetch(
        `/api/${projectSlug}/surveys/${surveyId}/available-relationships`,
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          }
        }
      );

      if (availableResponse.ok) {
        const availableData = await availableResponse.json();
        setAvailableRelationships(availableData);
      }
    } catch (error) {
      console.error('Error loading relationships:', error);
      toast.error('Failed to load relationships');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSaveChanges = async () => {
    if (!token) return;

    setIsLoading(true);
    try {
      // Determine which relationships to assign (newly selected from available)
      const toAssign = Array.from(selectedAvailable);

      // Determine which relationships to unassign (unchecked from assigned)
      const originalAssignedIds = new Set(assignedRelationships.map(r => r.SubjectEvaluatorId));
      const toUnassign = Array.from(originalAssignedIds).filter(id => !selectedAssigned.has(id));

      let assignedCount = 0;
      let unassignedCount = 0;

      // Assign new relationships
      if (toAssign.length > 0) {
        const assignResponse = await fetch(
          `/api/${projectSlug}/surveys/${surveyId}/assign-relationships`,
          {
            method: 'POST',
            headers: {
              'Authorization': `Bearer ${token}`,
              'Content-Type': 'application/json'
            },
            body: JSON.stringify({ SubjectEvaluatorIds: toAssign })
          }
        );

        if (assignResponse.ok) {
          const result = await assignResponse.json();
          assignedCount = result.AssignedCount || 0;
          if (result.Errors && result.Errors.length > 0) {
            result.Errors.forEach((error: string) => toast.error(error));
          }
        }
      }

      // Unassign relationships
      if (toUnassign.length > 0) {
        const unassignResponse = await fetch(
          `/api/${projectSlug}/surveys/${surveyId}/unassign-relationships`,
          {
            method: 'POST',
            headers: {
              'Authorization': `Bearer ${token}`,
              'Content-Type': 'application/json'
            },
            body: JSON.stringify({ SubjectEvaluatorIds: toUnassign })
          }
        );

        if (unassignResponse.ok) {
          const result = await unassignResponse.json();
          unassignedCount = result.UnassignedCount || 0;
        }
      }

      if (assignedCount > 0 || unassignedCount > 0) {
        toast.success(`Assigned: ${assignedCount}, Unassigned: ${unassignedCount}`);
        onAssignmentUpdated();
        onClose();
      } else {
        toast.success('No changes made');
      }
    } catch (error) {
      console.error('Error saving changes:', error);
      toast.error('Failed to save changes');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSelectAllAvailable = () => {
    const filtered = getFilteredAvailable();
    setSelectedAvailable(new Set(filtered.map(r => r.SubjectEvaluatorId)));
  };

  const handleDeselectAllAvailable = () => {
    setSelectedAvailable(new Set());
  };

  const handleSelectAllAssigned = () => {
    const filtered = getFilteredAssigned();
    setSelectedAssigned(new Set(filtered.map(r => r.SubjectEvaluatorId)));
  };

  const handleDeselectAllAssigned = () => {
    setSelectedAssigned(new Set());
  };

  const handleUploadButtonClick = () => {
    fileInputRef.current?.click();
  };

  const handleDownloadTemplate = () => {
    const header = 'EvaluatorId,SubjectId,Relationship\n';
    const example = 'has101,sal101,Direct\n';
    const csv = header + example;
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'assignment-template.csv';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
    toast.success('Assignment CSV template downloaded');
  };

  interface AssignmentCsvRow {
    EvaluatorId: string;
    SubjectId: string;
    Relationship: string;
  }

  const parseCsv = (content: string): AssignmentCsvRow[] => {
    const sanitizedContent = content.replace(/\r\n/g, '\n').trim();

    if (!sanitizedContent) {
      throw new Error('Assignment CSV is empty.');
    }

    const [rawHeader, ...rawRows] = sanitizedContent.split('\n');
    const header = rawHeader.replace('\uFEFF', '').trim().toLowerCase();
    const expectedHeader = 'evaluatorid,subjectid,relationship';

    if (header !== expectedHeader) {
      throw new Error(`Invalid header. Expected "${expectedHeader}" but received "${rawHeader.trim()}".`);
    }

    const rows: AssignmentCsvRow[] = [];

    rawRows.forEach((line, index) => {
      const trimmed = line.trim();
      if (!trimmed) {
        return;
      }

      const columns = trimmed.split(',').map(col => col.trim());

      if (columns.length < 3) {
        throw new Error(`Row ${index + 2} is missing values. Each row requires EvaluatorId, SubjectId, Relationship.`);
      }

      const [EvaluatorId, SubjectId, Relationship] = columns;

      rows.push({
        EvaluatorId,
        SubjectId,
        Relationship
      });
    });

    if (rows.length === 0) {
      throw new Error('No valid relationship rows were found in the CSV.');
    }

    return rows;
  };

  const handleCsvUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    if (!token) return;

    const file = event.target.files?.[0];
    event.target.value = '';

    if (!file) {
      return;
    }

    setIsCsvUploading(true);

    try {
      const text = await file.text();
      const rows = parseCsv(text);

      const response = await fetch(
        `/api/${projectSlug}/surveys/${surveyId}/assign-csv`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({ Rows: rows })
        }
      );

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data?.error || data?.message || 'Failed to process CSV assignments.');
      }

      toast.success(data?.message || `Successfully uploaded ${rows.length} CSV relationships.`);
      await loadRelationships();
      onAssignmentUpdated();
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to upload assignment CSV.';
      toast.error(message);
      console.error('Assignment CSV upload error:', error);
    } finally {
      setIsCsvUploading(false);
    }
  };

  const getFilteredAvailable = () => {
    return availableRelationships.filter(r => {
      const matchesSearch = !searchTerm || 
        r.SubjectFullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.EvaluatorFullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.SubjectEmployeeIdString.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.EvaluatorEmployeeIdString.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (r.SubjectDesignation?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false) ||
        (r.EvaluatorDesignation?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false);

      const matchesDesignation = !selectedDesignation || 
        r.SubjectDesignation === selectedDesignation || 
        r.EvaluatorDesignation === selectedDesignation;

      return matchesSearch && matchesDesignation;
    });
  };

  const getFilteredAssigned = () => {
    return assignedRelationships.filter(r => {
      const matchesSearch = !searchTerm || 
        r.SubjectFullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.EvaluatorFullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.SubjectEmployeeIdString.toLowerCase().includes(searchTerm.toLowerCase()) ||
        r.EvaluatorEmployeeIdString.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (r.SubjectDesignation?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false) ||
        (r.EvaluatorDesignation?.toLowerCase().includes(searchTerm.toLowerCase()) ?? false);

      const matchesDesignation = !selectedDesignation || 
        r.SubjectDesignation === selectedDesignation || 
        r.EvaluatorDesignation === selectedDesignation;

      return matchesSearch && matchesDesignation;
    });
  };

  const getAllDesignations = () => {
    const designations = new Set<string>();
    [...assignedRelationships, ...availableRelationships].forEach(r => {
      if (r.SubjectDesignation) designations.add(r.SubjectDesignation);
      if (r.EvaluatorDesignation) designations.add(r.EvaluatorDesignation);
    });
    return Array.from(designations).sort();
  };

  if (!isOpen) return null;

  const filteredAvailable = getFilteredAvailable();
  const filteredAssigned = getFilteredAssigned();
  const allDesignations = getAllDesignations();

  return (
    <div
      className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center z-50 p-4"
      data-designations-count={allDesignations.length}
    >
      <div className="bg-white rounded-lg shadow-xl w-full max-w-6xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-xl font-semibold text-gray-900">Assign Survey to Relationships</h2>
              <p className="text-sm text-gray-600 mt-1">
                {surveyTitle} {isSelfEvaluation && <span className="text-purple-600">(Self-Evaluation)</span>}
              </p>
            </div>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 transition-colors"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        {/* Search and Filter */}
        <div className="px-6 py-4 border-b border-gray-200 bg-gray-50">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:gap-4">
            <div className="flex-1 w-full">
              <input
                type="text"
                placeholder="Search by name, employee ID, or designation..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className={`w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500 ${styles.searchInput}`}
              />
            </div>
            <div className="w-full flex flex-col gap-3 lg:flex-row lg:w-auto lg:items-center">
              {/* <div className="w-full lg:w-64">
                <select
                  value={selectedDesignation}
                  onChange={(e) => setSelectedDesignation(e.target.value)}
                  className={`w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500`}
                >
                  <option value="">All Designations</option>
                  {allDesignations.map(designation => (
                    <option key={designation} value={designation}>{designation}</option>
                  ))}
                </select>
              </div> */}
              <div className="flex justify-start lg:justify-end">
                <button
                  type="button"
                  onClick={handleUploadButtonClick}
                  disabled={isCsvUploading || isLoading}
                  className="px-4 py-2 text-sm font-medium bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                >
                  {isCsvUploading && (
                    <span className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></span>
                  )}
                  Upload Assignment CSV
                </button>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".csv"
                  onChange={handleCsvUpload}
                  className="hidden"
                />
                <button
                  type="button"
                  onClick={handleDownloadTemplate}
                  disabled={isLoading}
                  className="ml-3 px-4 py-2 text-sm font-medium bg-white text-blue-700 border border-purple-200 rounded-md hover:bg-purple-50 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                >
                  Download Template
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div className="px-6 border-b border-gray-200">
          <div className="flex gap-4">
            <button
              onClick={() => setActiveTab('assigned')}
              className={`px-4 py-3 font-medium border-b-2 transition-colors ${
                activeTab === 'assigned'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
              Already Assigned ({filteredAssigned.length})
            </button>
            <button
              onClick={() => setActiveTab('available')}
              className={`px-4 py-3 font-medium border-b-2 transition-colors ${
                activeTab === 'available'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
              Available ({filteredAvailable.length})
            </button>
            
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto px-6 py-4">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            </div>
          ) : activeTab === 'available' ? (
            <div>
              {/* Bulk Actions for Available */}
              <div className="mb-4 flex gap-2">
                <button
                  onClick={handleSelectAllAvailable}
                  className="px-3 py-1 text-sm bg-blue-100 text-blue-700 rounded hover:bg-blue-200"
                >
                  Select All
                </button>
                <button
                  onClick={handleDeselectAllAvailable}
                  className="px-3 py-1 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200"
                >
                  Deselect All
                </button>
              </div>

              {filteredAvailable.length === 0 ? (
                <p className="text-center text-gray-500 py-8">No available relationships found</p>
              ) : (
                <div className="space-y-2">
                  {filteredAvailable.map(relationship => {
                    const isSelected = selectedAvailable.has(relationship.SubjectEvaluatorId);

                    const toggleSelection = () => {
                      const newSelected = new Set(selectedAvailable);
                      if (isSelected) {
                        newSelected.delete(relationship.SubjectEvaluatorId);
                      } else {
                        newSelected.add(relationship.SubjectEvaluatorId);
                      }
                      setSelectedAvailable(newSelected);
                    };

                    return (
                    <div
                      key={relationship.SubjectEvaluatorId}
                      className="flex items-start gap-3 p-4 border border-gray-200 rounded-lg hover:bg-gray-50 cursor-pointer"
                      onClick={toggleSelection}
                    >
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => {}} // Handled by parent div onClick
                        onClick={(e) => e.stopPropagation()}
                        className="mt-1 w-4 h-4 relative z-10 text-blue-600 border-gray-300 rounded focus:ring-blue-500 cursor-pointer"
                        style={{ pointerEvents: 'auto' }}
                      />
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <span className="font-medium text-gray-900">{relationship.SubjectFullName}</span>
                          <span className="text-gray-400">is evaluated by</span>
                          <span className="font-medium text-gray-900">{relationship.EvaluatorFullName}</span>
                          <span className="px-2 py-1 text-xs bg-blue-100 text-blue-800 rounded-full">
                            {relationship.Relationship}
                          </span>
                        </div>
                        <div className="text-sm text-gray-600 mt-1">
                          {relationship.SubjectEmployeeIdString} • {relationship.SubjectEmail}
                          {relationship.SubjectDesignation && ` • ${relationship.SubjectDesignation}`}
                        </div>
                      </div>
                    </div>
                    );
                  })}
                </div>
              )}
            </div>
          ) : (
            <div>
              {/* Bulk Actions for Assigned */}
              <div className="mb-4 flex gap-2">
                <button
                  onClick={handleSelectAllAssigned}
                  className="px-3 py-1 text-sm bg-blue-100 text-blue-700 rounded hover:bg-blue-200"
                >
                  Select All
                </button>
                <button
                  onClick={handleDeselectAllAssigned}
                  className="px-3 py-1 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200"
                >
                  Deselect All
                </button>
              </div>

              {filteredAssigned.length === 0 ? (
                <p className="text-center text-gray-500 py-8">No assigned relationships found</p>
              ) : (
                <div className="space-y-2">
                  {filteredAssigned.map(relationship => {
                    const isSelected = selectedAssigned.has(relationship.SubjectEvaluatorId);

                    const toggleSelection = () => {
                      const newSelected = new Set(selectedAssigned);
                      if (isSelected) {
                        newSelected.delete(relationship.SubjectEvaluatorId);
                      } else {
                        newSelected.add(relationship.SubjectEvaluatorId);
                      }
                      setSelectedAssigned(newSelected);
                    };

                    return (
                    <div
                      key={relationship.SubjectEvaluatorId}
                      className="flex items-start gap-3 p-4 border border-green-200 bg-green-50 rounded-lg hover:bg-green-100 cursor-pointer"
                      onClick={toggleSelection}
                    >
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => {}} // Handled by parent div onClick
                        onClick={(e) => e.stopPropagation()}
                        className="mt-1 w-4 h-4 relative z-10 text-blue-600 border-gray-300 rounded focus:ring-blue-500 cursor-pointer"
                        style={{ pointerEvents: 'auto' }}
                      />
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <span className="font-medium text-gray-900">{relationship.SubjectFullName}</span>
                          <span className="text-gray-400">is evaluated by</span>
                          <span className="font-medium text-gray-900">{relationship.EvaluatorFullName}</span>
                          <span className="px-2 py-1 text-xs bg-green-600 text-white rounded-full">
                            {relationship.Relationship}
                          </span>
                        </div>
                        <div className="text-sm text-gray-600 mt-1">
                          {relationship.SubjectEmployeeIdString} • {relationship.SubjectEmail}
                          {relationship.SubjectDesignation && ` • ${relationship.SubjectDesignation}`}
                        </div>
                      </div>
                    </div>
                    );
                  })}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-gray-200 bg-gray-50 flex justify-between items-center">
          <div className="text-sm text-gray-600">
            {activeTab === 'available' 
              ? `${selectedAvailable.size} selected`
              : `${selectedAssigned.size} of ${assignedRelationships.length} selected`
            }
          </div>
          <div className="flex gap-3">
            <button
              onClick={onClose}
              disabled={isLoading}
              className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              onClick={handleSaveChanges}
              disabled={isLoading}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2"
            >
              {isLoading && (
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
              )}
              Save Changes
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

