'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';

interface RelationshipWithSurveys {
  Id: string;
  SubjectId: string;
  EvaluatorId: string;
  Relationship: string;
  SubjectFullName: string;
  EvaluatorFullName: string;
  SurveyAssignments: {
    SurveyId: string;
    SurveyTitle: string;
  }[];
}

interface ViewRelationshipsModalProps {
  isOpen: boolean;
  onClose: () => void;
  entityType: 'subject' | 'evaluator';
  entityId: string | null;
  entityName: string;
  projectSlug: string;
}

export default function ViewRelationshipsModal({
  isOpen,
  onClose,
  entityType,
  entityId,
  entityName,
  projectSlug
}: ViewRelationshipsModalProps) {
  const { token } = useAuth();
  const [relationships, setRelationships] = useState<RelationshipWithSurveys[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (isOpen && entityId) {
      loadRelationships();
    }
  }, [isOpen, entityId, entityType, projectSlug]);

  const loadRelationships = async () => {
    if (!token || !entityId) return;

    setIsLoading(true);
    try {
      const endpoint = entityType === 'subject'
        ? `/api/${projectSlug}/subject-evaluators/subjects/${entityId}/relationships-with-surveys`
        : `/api/${projectSlug}/subject-evaluators/evaluators/${entityId}/relationships-with-surveys`;

      const response = await fetch(endpoint, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to load relationships');
      }

      const data = await response.json();
      setRelationships(data || []);
    } catch (error) {
      console.error('Error loading relationships:', error);
      toast.error('Failed to load relationships');
      setRelationships([]);
    } finally {
      setIsLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-xl font-semibold text-gray-900">Relationships & Survey Assignments</h2>
              <p className="text-sm text-gray-600 mt-1">
                {entityName} ({entityType === 'subject' ? 'Subject' : 'Evaluator'})
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

        {/* Content */}
        <div className="flex-1 overflow-y-auto px-6 py-4">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            </div>
          ) : relationships.length === 0 ? (
            <div className="text-center py-12 text-gray-500">
              No relationships found for this {entityType}.
            </div>
          ) : (
            <div className="space-y-3">
              {relationships.map((relationship) => (
                <div
                  key={relationship.Id}
                  className="border border-gray-200 rounded-lg p-4 hover:bg-gray-50 transition-colors bg-white"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 flex-wrap">
                        <span className="font-medium text-gray-900 text-base">
                          {relationship.SubjectFullName} - {relationship.EvaluatorFullName}
                        </span>
                        <span className="px-2.5 py-1 text-xs font-medium bg-blue-100 text-blue-800 rounded-full">
                          {relationship.Relationship}
                        </span>
                      </div>
                      
                      {relationship.SurveyAssignments.length > 0 ? (
                        <div className="mt-3 flex flex-wrap items-center gap-2">
                          <span className="text-sm text-gray-600 font-medium">Surveys:</span>
                          {relationship.SurveyAssignments.map((assignment) => (
                            <span
                              key={assignment.SurveyId}
                              className="px-3 py-1 text-sm bg-purple-100 text-purple-800 rounded-full font-medium"
                            >
                              {assignment.SurveyTitle}
                            </span>
                          ))}
                        </div>
                      ) : (
                        <div className="mt-3">
                          <span className="text-sm text-gray-500 italic">Not assigned to any surveys</span>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-gray-200 bg-gray-50 flex justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}

