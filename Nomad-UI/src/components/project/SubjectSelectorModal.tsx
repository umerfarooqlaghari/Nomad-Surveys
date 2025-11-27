'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { subjectService, SubjectListResponse } from '@/services/subjectService';
import toast from 'react-hot-toast';

interface SubjectSelectorModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (selectedSubjectIds: string[]) => void;
  projectSlug: string;
}

export default function SubjectSelectorModal({
  isOpen,
  onClose,
  onConfirm,
  projectSlug,
}: SubjectSelectorModalProps) {
  const { token } = useAuth();
  const [subjects, setSubjects] = useState<SubjectListResponse[]>([]);
  const [selectedSubjectIds, setSelectedSubjectIds] = useState<string[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen && token) {
      loadSubjects();
    }
  }, [isOpen, token, projectSlug]);

  const loadSubjects = async () => {
    if (!token) return;

    setLoading(true);
    try {
      const response = await subjectService.getSubjects(projectSlug, token);

      if (response.error) {
        toast.error(response.error);
        setSubjects([]);
      } else {
        setSubjects(response.data || []);
      }
    } catch (error) {
      console.error('Error loading subjects:', error);
      toast.error('Failed to load subjects');
      setSubjects([]);
    } finally {
      setLoading(false);
    }
  };

  const toggleSubject = (subjectId: string) => {
    setSelectedSubjectIds((prev) =>
      prev.includes(subjectId)
        ? prev.filter((id) => id !== subjectId)
        : [...prev, subjectId]
    );
  };

  const handleSelectAll = () => {
    const filteredSubjects = getFilteredSubjects();
    if (selectedSubjectIds.length === filteredSubjects.length) {
      setSelectedSubjectIds([]);
    } else {
      setSelectedSubjectIds(filteredSubjects.map((s) => s.Id));
    }
  };

  const getFilteredSubjects = () => {
    if (!searchTerm) return subjects;
    const term = searchTerm.toLowerCase();
    return subjects.filter(
      (subject) =>
        subject.FullName?.toLowerCase().includes(term) ||
        subject.EmployeeIdString?.toLowerCase().includes(term) 
    );
  };

  const handleConfirm = () => {
    if (selectedSubjectIds.length === 0) {
      toast.error('Please select at least one subject');
      return;
    }
    onConfirm(selectedSubjectIds);
    setSelectedSubjectIds([]);
    setSearchTerm('');
  };

  const handleClose = () => {
    setSelectedSubjectIds([]);
    setSearchTerm('');
    onClose();
  };

  if (!isOpen) return null;

  const filteredSubjects = getFilteredSubjects();

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-3xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
          <h2 className="text-xl font-bold text-gray-900">Select Subjects</h2>
          <button
            onClick={handleClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Search */}
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex items-center gap-4">
            <input
              type="text"
              placeholder="Search by name, employee ID, department, or position..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="flex-1 px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <button
              onClick={handleSelectAll}
              className="px-4 py-2 bg-gray-100 hover:bg-gray-200 rounded-md text-sm font-medium transition-colors"
            >
              {selectedSubjectIds.length === filteredSubjects.length ? 'Deselect All' : 'Select All'}
            </button>
          </div>
          <div className="mt-2 text-sm text-gray-600">
            {selectedSubjectIds.length} of {filteredSubjects.length} selected
          </div>
        </div>

        {/* Subjects List */}
        <div className="flex-1 overflow-y-auto px-6 py-4">
          {loading ? (
            <div className="flex items-center justify-center py-12">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            </div>
          ) : filteredSubjects.length === 0 ? (
            <div className="text-center py-12 text-gray-500">
              {searchTerm ? 'No subjects found matching your search' : 'No subjects available'}
            </div>
          ) : (
            <div className="space-y-2">
              {filteredSubjects.map((subject) => {
                const isSelected = selectedSubjectIds.includes(subject.Id);
                return (
                  <div
                    key={subject.Id}
                    onClick={() => toggleSubject(subject.Id)}
                    className={`p-4 border rounded-lg cursor-pointer transition-colors ${
                      isSelected
                        ? 'border-blue-500 bg-blue-50'
                        : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                    }`}
                  >
                    <div className="flex items-center gap-3">
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => toggleSubject(subject.Id)}
                        className="w-5 h-5 text-blue-600 rounded focus:ring-blue-500 cursor-pointer"
                        onClick={(e) => e.stopPropagation()}
                      />
                      <div className="flex-1">
                        <div className="font-medium text-gray-900">{subject.FullName || 'N/A'}</div>
                        <div className="text-sm text-gray-600 mt-1">
                          {subject.EmployeeIdString && <span>ID: {subject.EmployeeIdString}</span>}
                          {(subject.Designation) && (
                            <span className="ml-4">
                              {subject.Designation ? `Position: ${subject.Designation}` : ''}
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-gray-200 flex items-center justify-end gap-3">
          <button
            onClick={handleClose}
            className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleConfirm}
            disabled={selectedSubjectIds.length === 0}
            className="px-6 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed text-white rounded-md text-sm font-medium transition-colors"
          >
            Confirm ({selectedSubjectIds.length} selected)
          </button>
        </div>
      </div>
    </div>
  );
}

