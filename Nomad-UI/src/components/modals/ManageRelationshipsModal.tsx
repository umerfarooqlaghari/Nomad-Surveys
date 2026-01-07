'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';

interface SubjectSummary {
  Id: string;
  EmployeeId: string;
  FirstName: string;
  LastName: string;
  FullName: string;
  Email: string;
  EmployeeIdString: string;
  Designation?: string;
  IsActive: boolean;
}

interface EvaluatorSummary {
  Id: string;
  EmployeeId: string;
  FirstName: string;
  LastName: string;
  FullName: string;
  Email: string;
  EmployeeIdString: string;
  Designation?: string;
  IsActive: boolean;
}

interface Relationship {
  Id: string;
  SubjectId: string;
  EvaluatorId: string;
  Relationship: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  Subject?: SubjectSummary;
  Evaluator?: EvaluatorSummary;
}

interface AvailableOption {
  Id: string;
  EmployeeId: string;
  FullName: string;
  Email: string;
  EmployeeIdString: string;
  Designation?: string;
}

interface ManageRelationshipsModalProps {
  isOpen: boolean;
  onClose: () => void;
  entityType: 'subject' | 'evaluator';
  entityId: string | null;
  entityName: string;
  projectSlug: string;
  onRelationshipsUpdated: () => void;
}

const relationshipTypes = [
  'Self',
  'Manager',
  'Direct Report',
  'Peer',
  'Stakeholder',
  'Skipline'
];

export default function ManageRelationshipsModal({
  isOpen,
  onClose,
  entityType,
  entityId,
  entityName,
  projectSlug,
  onRelationshipsUpdated
}: ManageRelationshipsModalProps) {
  const { token } = useAuth();
  const [activeTab, setActiveTab] = useState<'existing' | 'add'>('existing');
  const [existingRelationships, setExistingRelationships] = useState<Relationship[]>([]);
  const [availableOptions, setAvailableOptions] = useState<AvailableOption[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedRelationships, setSelectedRelationships] = useState<{ [key: string]: string }>({});
  const [editingRelationship, setEditingRelationship] = useState<{ id: string; relationship: string } | null>(null);
  const [currentEntity, setCurrentEntity] = useState<AvailableOption | null>(null);
  

  useEffect(() => {
    if (isOpen && entityId) {
      loadExistingRelationships();
      loadAvailableOptions();
      loadCurrentEntity();
      // Clear selected relationships when modal opens
      setSelectedRelationships({});
    }
  }, [isOpen, entityId]);

  // Clear selected relationships when switching tabs
  useEffect(() => {
    setSelectedRelationships({});
  }, [activeTab]);

  // Auto-select self-evaluation relationships with "Self" type when tab changes
  // DISABLED: This was causing issues where self-relationships were auto-selected even when already existing
  // useEffect(() => {
  //   if (currentEntity && availableOptions.length > 0 && activeTab === 'add') {
  //     const selfOption = availableOptions.find(opt => opt.EmployeeId === currentEntity.EmployeeId);
  //     if (selfOption) {
  //       setSelectedRelationships(prev => {
  //         if (!prev[selfOption.Id]) {
  //           return {
  //             ...prev,
  //             [selfOption.Id]: 'Self'
  //           };
  //         }
  //         return prev;
  //       });
  //     }
  //   }
  // }, [currentEntity, availableOptions, activeTab]);



  const loadExistingRelationships = async () => {
    if (!token || !entityId) return;

    setIsLoading(true);
    try {
      const endpoint = entityType === 'subject'
        ? `/api/${projectSlug}/subject-evaluators/subjects/${entityId}/evaluators`
        : `/api/${projectSlug}/subject-evaluators/evaluators/${entityId}/subjects`;

      const response = await fetch(endpoint, {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (response.ok) {
        const data = await response.json();
        setExistingRelationships(data || []);
      } else {
        toast.error('Failed to load existing relationships');
      }
    } catch (error) {
      console.error('Error loading relationships:', error);
      toast.error('Failed to load relationships');
    } finally {
      setIsLoading(false);
    }
  };

  const loadCurrentEntity = async () => {
    if (!token || !entityId) return;

    try {
      const endpoint = entityType === 'subject'
        ? `/api/${projectSlug}/subjects/${entityId}`
        : `/api/${projectSlug}/evaluators/${entityId}`;

      const response = await fetch(endpoint, {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (response.ok) {
        const data = await response.json();
        setCurrentEntity({
          Id: data.Id,
          EmployeeId: data.EmployeeId,
          FullName: data.FullName,
          Email: data.Email,
          EmployeeIdString: data.EmployeeIdString,
          Designation: data.Designation
        });
      }
    } catch (error) {
      console.error('Error loading current entity:', error);
    }
  };

  const loadAvailableOptions = async () => {
    if (!token) return;

    try {
      const endpoint = entityType === 'subject'
        ? `/api/${projectSlug}/evaluators`
        : `/api/${projectSlug}/subjects`;

      const response = await fetch(endpoint, {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (response.ok) {
        const data = await response.json();
        setAvailableOptions(data || []);
      }
    } catch (error) {
      console.error('Error loading available options:', error);
    }
  };

  const handleAddRelationships = async () => {
    if (!token || !entityId) {
      toast.error('Invalid request');
      return;
    }

    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const relationshipsToAdd = Object.entries(selectedRelationships).filter(([_, rel]) => rel !== '');

    if (relationshipsToAdd.length === 0) {
      toast.error('Please select at least one relationship');
      return;
    }

    setIsLoading(true);
    try {
      const endpoint = entityType === 'subject'
        ? `/api/${projectSlug}/subject-evaluators/subjects/${entityId}/evaluators`
        : `/api/${projectSlug}/subject-evaluators/evaluators/${entityId}/subjects`;

      const payload = entityType === 'subject'
        ? { Evaluators: relationshipsToAdd.map(([id, rel]) => ({ EvaluatorId: id, Relationship: rel })) }
        : { Subjects: relationshipsToAdd.map(([id, rel]) => ({ SubjectId: id, Relationship: rel })) };

      const response = await fetch(endpoint, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        toast.success(`Successfully added ${relationshipsToAdd.length} relationship(s)`);
        setSelectedRelationships({});
        setSearchTerm('');
        await loadExistingRelationships();
        setActiveTab('existing');
        onRelationshipsUpdated();
      } else {
        const error = await response.json();
        toast.error(error.message || 'Failed to add relationships');
      }
    } catch (error) {
      console.error('Error adding relationships:', error);
      toast.error('Failed to add relationships');
    } finally {
      setIsLoading(false);
    }
  };

  const handleUpdateRelationship = async (relationshipId: string, newRelationshipType: string) => {
    if (!token || !entityId) return;

    setIsLoading(true);
    try {
      const relationship = existingRelationships.find(r => r.Id === relationshipId);
      if (!relationship) return;

      const endpoint = entityType === 'subject'
        ? `/api/${projectSlug}/subject-evaluators/subjects/${entityId}/evaluators/${relationship.EvaluatorId}`
        : `/api/${projectSlug}/subject-evaluators/subjects/${relationship.SubjectId}/evaluators/${entityId}`;

      const response = await fetch(endpoint, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ Relationship: newRelationshipType })
      });

      if (response.ok) {
        toast.success('Relationship updated successfully');
        setEditingRelationship(null);
        await loadExistingRelationships();
        onRelationshipsUpdated();
      } else {
        toast.error('Failed to update relationship');
      }
    } catch (error) {
      console.error('Error updating relationship:', error);
      toast.error('Failed to update relationship');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteRelationship = async (relationshipId: string) => {
    if (!token || !entityId || !confirm('Are you sure you want to delete this relationship?')) return;

    setIsLoading(true);
    try {
      const relationship = existingRelationships.find(r => r.Id === relationshipId);
      if (!relationship) return;

      const endpoint = entityType === 'subject'
        ? `/api/${projectSlug}/subject-evaluators/subjects/${entityId}/evaluators/${relationship.EvaluatorId}`
        : `/api/${projectSlug}/subject-evaluators/subjects/${relationship.SubjectId}/evaluators/${entityId}`;

      const response = await fetch(endpoint, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (response.ok) {
        toast.success('Relationship deleted successfully');
        await loadExistingRelationships();
        onRelationshipsUpdated();
      } else {
        toast.error('Failed to delete relationship');
      }
    } catch (error) {
      console.error('Error deleting relationship:', error);
      toast.error('Failed to delete relationship');
    } finally {
      setIsLoading(false);
    }
  };

  const handleRelationshipChange = (id: string, relationship: string) => {
    setSelectedRelationships(prev => {
      if (relationship === '') {
        const newState = { ...prev };
        delete newState[id];
        return newState;  
      }
      return { ...prev, [id]: relationship };
    });
  };

  // Filter available options
  const existingIds = existingRelationships.map(r =>
    entityType === 'subject' ? r.EvaluatorId : r.SubjectId
  );

  // Get existing EmployeeIds to check for self-relationships
  const existingEmployeeIds = existingRelationships.map(r =>
    entityType === 'subject' ? r.Evaluator?.EmployeeId : r.Subject?.EmployeeId
  ).filter(Boolean) as string[];

  // Include current entity in available options for self-evaluation
  // Check by EmployeeId to avoid duplicates (same employee can be both subject and evaluator with different IDs)
  const allAvailableOptions = currentEntity && !availableOptions.some(opt => opt.EmployeeId === currentEntity.EmployeeId)
    ? [...availableOptions, currentEntity]
    : availableOptions;

  const filteredAvailableOptions = allAvailableOptions
    .filter(option => {
      // For self-relationships, check by EmployeeId instead of Id
      if (currentEntity && option.EmployeeId === currentEntity.EmployeeId) {
        return !existingEmployeeIds.includes(option.EmployeeId);
      }
      // For other relationships, check by Id as usual
      return !existingIds.includes(option.Id);
    })
    .filter(option =>
      searchTerm === '' ||
      option.FullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      option.Email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      option.EmployeeIdString.toLowerCase().includes(searchTerm.toLowerCase())
    );

  // Auto-suggest "Self" relationship for current entity (but don't force it)
  // Find the option that matches the current entity's EmployeeId (could be different Id if same person is both subject and evaluator)
  // Note: We no longer auto-select this - admins can choose to add or remove self-evaluation relationships
  useEffect(() => {
    // This effect is kept for potential future use but doesn't auto-select anymore
    // Admins have full control over all relationships including self-evaluation
  }, [currentEntity, availableOptions, existingRelationships]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 flex items-center justify-center z-50 p-4 overflow-y-auto backdrop-blur-sm bg-black/20">
      <div className="bg-white/90 rounded-lg shadow-2xl max-w-5xl w-full mt-20 flex flex-col" style={{ maxHeight: 'calc(100vh - 8rem)' }}>
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200 bg-blue-600">
          <div className="flex justify-between items-center">
            <div>
              <h2 className="text-2xl font-bold text-white">Manage Relationships</h2>
              <p className="text-blue-100 text-sm mt-1">
                {entityType === 'subject' ? 'Subject' : 'Evaluator'}: <span className="font-semibold">{entityName}</span>
              </p>
            </div>
            <button
              onClick={onClose}
              className="text-white hover:text-gray-200 transition-colors"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        {/* Tabs */}
        <div className="flex border-b border-gray-200 bg-gray-50">
          <button
            onClick={() => setActiveTab('existing')}
            className={`flex-1 px-6 py-3 text-sm font-medium transition-colors ${
              activeTab === 'existing'
                ? 'text-blue-600 border-b-2 border-blue-600 bg-white'
                : 'text-gray-600 hover:text-gray-800 hover:bg-gray-100'
            }`}
          >
            Existing Relationships ({existingRelationships.length})
          </button>
          <button
            onClick={() => setActiveTab('add')}
            className={`flex-1 px-6 py-3 text-sm font-medium transition-colors ${
              activeTab === 'add'
                ? 'text-blue-600 border-b-2 border-blue-600 bg-white'
                : 'text-gray-600 hover:text-gray-800 hover:bg-gray-100'
            }`}
          >
            Add New Relationship
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6">
          {activeTab === 'existing' ? (
            <div>
              {isLoading ? (
                <div className="flex justify-center items-center py-12">
                  <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
                </div>
              ) : existingRelationships.length === 0 ? (
                <div className="text-center py-12">
                  <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                  </svg>
                  <p className="mt-4 text-gray-500">No relationships found</p>
                  <p className="text-sm text-gray-400 mt-2">Click &quot;Add New Relationship&quot; to get started</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {existingRelationships.map((rel) => (
                    <div key={rel.Id} className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow bg-white">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-3">
                            <div className="w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center">
                              <span className="text-blue-600 font-semibold text-sm">
                                {entityType === 'subject'
                                  ? rel.Evaluator?.FullName?.charAt(0) || 'E'
                                  : rel.Subject?.FullName?.charAt(0) || 'S'}
                              </span>
                            </div>
                            <div>
                              <h4 className="font-semibold text-gray-900">
                                {entityType === 'subject' ? rel.Evaluator?.FullName : rel.Subject?.FullName}
                              </h4>
                              <p className="text-sm text-gray-600">
                                {entityType === 'subject' ? rel.Evaluator?.Email : rel.Subject?.Email}
                                {' • '}
                                <span className="text-gray-500">
                                  {entityType === 'subject' ? rel.Evaluator?.EmployeeIdString : rel.Subject?.EmployeeIdString}
                                </span>
                              </p>
                            </div>
                          </div>
                          <div className="mt-3 ml-13">
                            {editingRelationship?.id === rel.Id ? (
                              <div className="flex items-center gap-2">
                                <select
                                  value={editingRelationship.relationship}
                                  onChange={(e) => setEditingRelationship({ ...editingRelationship, relationship: e.target.value })}
                                  className="border border-gray-300 rounded-md px-3 py-1 text-sm"
                                >
                                  {relationshipTypes.map(type => (
                                    <option key={type} value={type}>{type}</option>
                                  ))}
                                </select>
                                <button
                                  onClick={() => handleUpdateRelationship(rel.Id, editingRelationship.relationship)}
                                  className="px-3 py-1 bg-green-600 text-white rounded-md text-sm hover:bg-green-700"
                                >
                                  Save
                                </button>
                                <button
                                  onClick={() => setEditingRelationship(null)}
                                  className="px-3 py-1 bg-gray-300 text-gray-700 rounded-md text-sm hover:bg-gray-400"
                                >
                                  Cancel
                                </button>
                              </div>
                            ) : (
                              <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-blue-100 text-blue-800">
                                {rel.Relationship}
                              </span>
                            )}
                          </div>
                        </div>
                        <div className="flex gap-2">
                          {!editingRelationship && (
                            <>
                              <button
                                onClick={() => setEditingRelationship({ id: rel.Id, relationship: rel.Relationship })}
                                className="text-blue-600 hover:text-blue-800 p-2"
                                title="Edit relationship"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                                </svg>
                              </button>
                              <button
                                onClick={() => handleDeleteRelationship(rel.Id)}
                                className="text-red-600 hover:text-red-800 p-2"
                                title="Delete relationship"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                </svg>
                              </button>
                            </>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ) : (
            <div>
              {/* Search */}
              <div className="mb-4">
                <input
                  type="text"
                  placeholder={`Search ${entityType === 'subject' ? 'evaluators' : 'subjects'}...`}
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-full border border-gray-300 rounded-md px-4 py-2 text-gray-900 bg-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              {/* Summary */}
              <div className="mb-4 bg-blue-50 p-3 rounded-md">
                <span className="text-sm font-medium text-blue-900">
                  {Object.keys(selectedRelationships).length} relationship(s) selected
                </span>
              </div>

              {/* Two Column Layout: User Selection | Relationship Type */}
              <div className="border border-gray-300 rounded-md bg-white overflow-hidden">
                {/* Header Row */}
                <div className="grid grid-cols-2 bg-gray-50 border-b border-gray-300">
                  <div className="px-4 py-3 font-semibold text-sm text-gray-700 border-r border-gray-300">
                    Select {entityType === 'subject' ? 'Evaluator' : 'Subject'}
                  </div>
                  <div className="px-4 py-3 font-semibold text-sm text-gray-700">
                    Relationship Type
                  </div>
                </div>

                {/* Scrollable Content */}
                <div className="max-h-96 overflow-y-auto">
                  {filteredAvailableOptions.length === 0 ? (
                    <div className="p-8 text-center text-gray-500">
                      {searchTerm ? 'No matching options found' : `All ${entityType === 'subject' ? 'evaluators' : 'subjects'} are already connected`}
                    </div>
                  ) : (
                    filteredAvailableOptions.map(option => (
                      <div
                        key={option.Id}
                        className="grid grid-cols-2 border-b border-gray-200 hover:bg-gray-50"
                      >
                        {/* User Info Column */}
                        <div className="px-4 py-3 border-r border-gray-200">
                          <div className="font-medium text-gray-900">
                            {option.FullName}
                            {currentEntity && option.EmployeeId === currentEntity.EmployeeId && (
                              <span className="ml-2 text-xs bg-purple-100 text-purple-800 px-2 py-1 rounded-full">
                                Self
                              </span>
                            )}
                          </div>
                          <div className="text-sm text-gray-600">
                            {option.EmployeeIdString} • {option.Email}
                          </div>
                          {option.Designation && (
                            <div className="text-xs text-gray-500 mt-1">
                              {option.Designation}
                            </div>
                          )}
                        </div>

                        {/* Relationship Dropdown Column */}
                        <div className="px-4 py-3 flex items-center gap-3">
                          {currentEntity && option.EmployeeId === currentEntity.EmployeeId ? (
                            <>
                              {/* Checkbox to enable/disable self-evaluation */}
                              <div
                                className="flex items-center cursor-pointer"
                                onClick={() => {
                                  if (selectedRelationships[option.Id]) {
                                    handleRelationshipChange(option.Id, '');
                                  } else {
                                    handleRelationshipChange(option.Id, 'Self');
                                  }
                                }}
                              >
                                <input
                                  type="checkbox"
                                  checked={!!selectedRelationships[option.Id]}
                                  onChange={() => {}}
                                  // onClick={(e) => e.stopPropagation()}
                                  className="w-4 h-4 relative z-10 text-blue-600 border-gray-300 rounded focus:ring-blue-500 cursor-pointer"
                                />
                              </div>
                              <select
                                value="Self"
                                disabled
                                className="flex-1 border border-gray-300 rounded-md px-3 py-2 text-sm text-gray-900 bg-gray-100 cursor-not-allowed"
                              >
                                <option value="Self">Self</option>
                              </select>
                            </>
                          ) : (
                            <select
                              value={selectedRelationships[option.Id] || ''}
                              onChange={(e) => handleRelationshipChange(option.Id, e.target.value)}
                              className="flex-1 border border-gray-300 rounded-md px-3 py-2 text-sm text-gray-900 bg-white focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                            >
                              <option value="">Select Relationship</option>
                              {relationshipTypes.map(type => (
                                <option key={type} value={type}>{type}</option>
                              ))}
                            </select>
                          )}
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        {activeTab === 'add' && (
          <div className="px-6 py-4 border-t border-gray-200 bg-gray-50 flex justify-end gap-3">
            <button
              onClick={onClose}
              className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-100 font-medium"
            >
              Cancel
            </button>
            <button
              onClick={handleAddRelationships}
              disabled={Object.keys(selectedRelationships).length === 0 || isLoading}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 font-medium disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? 'Adding...' : `Add ${Object.keys(selectedRelationships).length} Relationship${Object.keys(selectedRelationships).length !== 1 ? 's' : ''}`}
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

