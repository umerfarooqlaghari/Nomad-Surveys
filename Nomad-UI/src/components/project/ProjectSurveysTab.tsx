/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import CustomSurveyBuilder from '@/components/survey/CustomSurveyBuilder';
import AssignSurveyModal from '@/components/modals/AssignSurveyModal';
import toast from 'react-hot-toast';

interface ProjectSurveysTabProps {
  projectSlug: string;
}

interface Survey {
  Id: string;
  Title: string;
  Description?: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  QuestionCount: number;
  IsSelfEvaluation: boolean;
  AssignmentCount?: number;
}

export default function ProjectSurveysTab({ projectSlug }: ProjectSurveysTabProps) {
  const { token } = useAuth();
  const [surveys, setSurveys] = useState<Survey[]>([]);
  const [loading, setLoading] = useState(true);
  const [showBuilder, setShowBuilder] = useState(false);
  const [editingSurvey, setEditingSurvey] = useState<any>(null);
  const [showAssignModal, setShowAssignModal] = useState(false);
  const [selectedSurvey, setSelectedSurvey] = useState<Survey | null>(null);
  const [assignmentCounts, setAssignmentCounts] = useState<Record<string, number>>({});

  useEffect(() => {
    fetchSurveys();
  }, []);

  const fetchSurveys = async () => {
    try {
      setLoading(true);
      const response = await fetch(`/api/${projectSlug}/surveys`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to fetch surveys');
      }

      const data = await response.json();
      setSurveys(data);

      // Fetch assignment counts for all surveys
      await fetchAssignmentCounts(data);
    } catch (error) {
      console.error('Error fetching surveys:', error);
      toast.error('Failed to load surveys');
    } finally {
      setLoading(false);
    }
  };

  const fetchAssignmentCounts = async (surveyList: Survey[]) => {
    try {
      const counts: Record<string, number> = {};

      await Promise.all(
        surveyList.map(async (survey) => {
          try {
            const response = await fetch(
              `/api/${projectSlug}/surveys/${survey.Id}/assignment-count`,
              {
                headers: {
                  Authorization: `Bearer ${token}`,
                },
              }
            );

            if (response.ok) {
              const count = await response.json();
              counts[survey.Id] = count;
            }
          } catch (error) {
            console.error(`Error fetching assignment count for survey ${survey.Id}:`, error);
          }
        })
      );

      setAssignmentCounts(counts);
    } catch (error) {
      console.error('Error fetching assignment counts:', error);
    }
  };

  const handleCreateSurvey = () => {
    setEditingSurvey(null);
    setShowBuilder(true);
  };

  const handleEditSurvey = async (surveyId: string) => {
    try {
      const response = await fetch(`/api/${projectSlug}/surveys/${surveyId}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to fetch survey');
      }

      const survey = await response.json();
      setEditingSurvey(survey);
      setShowBuilder(true);
    } catch (error) {
      console.error('Error fetching survey:', error);
      toast.error('Failed to load survey');
    }
  };

  const handleDeleteSurvey = async (surveyId: string) => {
    if (!confirm('Are you sure you want to delete this survey?')) {
      return;
    }

    try {
      const response = await fetch(`/api/${projectSlug}/surveys/${surveyId}`, {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to delete survey');
      }

      toast.success('Survey deleted successfully');
      fetchSurveys();
    } catch (error) {
      console.error('Error deleting survey:', error);
      toast.error('Failed to delete survey');
    }
  };

  const handleSaveSurvey = () => {
    setShowBuilder(false);
    setEditingSurvey(null);
    fetchSurveys();
  };

  const handleCancelBuilder = () => {
    setShowBuilder(false);
    setEditingSurvey(null);
  };

  const handleAssignSurvey = (survey: Survey) => {
    setSelectedSurvey(survey);
    setShowAssignModal(true);
  };

  const handleAssignmentUpdated = () => {
    fetchSurveys();
  };

  if (showBuilder) {
    return (
      <CustomSurveyBuilder
        tenantSlug={projectSlug}
        token={token || ''}
        initialSurvey={editingSurvey}
        surveyId={editingSurvey?.Id}
        onSave={handleSaveSurvey}
        onCancel={handleCancelBuilder}
      />
    );
  }

  return (
    <div className="space-y-6">
      <div className="bg-white shadow rounded-lg p-6">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text-2xl font-bold text-gray-900">Surveys</h2>
            <p className="text-gray-600">Manage surveys for project: {projectSlug}</p>
          </div>
          <div className="flex space-x-3">
            <button
              onClick={handleCreateSurvey}
              className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium"
            >
              Create Survey
            </button>
          </div>
        </div>

        {/* Active Surveys */}
        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4">
            {loading ? 'Loading surveys...' : `Surveys (${surveys.length})`}
          </h3>
          {loading ? (
            <div className="bg-gray-50 rounded-lg p-8 text-center">
              <div className="animate-pulse">
                <div className="h-4 bg-gray-300 rounded w-1/4 mx-auto"></div>
              </div>
            </div>
          ) : surveys.length === 0 ? (
            <div className="bg-gray-50 rounded-lg p-8 text-center">
              <p className="text-gray-500 mb-4">No surveys created yet</p>
              <button
                onClick={handleCreateSurvey}
                className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm"
              >
                Create Your First Survey
              </button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Survey Title
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Description
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Questions
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Assignments
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Created
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {surveys.map((survey) => (
                    <tr key={survey.Id}>
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          <div className="text-sm font-medium text-gray-900">{survey.Title}</div>
                          {survey.IsSelfEvaluation && (
                            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-purple-100 text-purple-800">
                              Self-Evaluation
                            </span>
                          )}
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <div className="text-sm text-gray-500 max-w-xs truncate">
                          {survey.Description || 'No description'}
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {survey.QuestionCount}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-2">
                          <span className="text-sm text-gray-900 font-medium">
                            {assignmentCounts[survey.Id] || 0}
                          </span>
                          <button
                            onClick={() => handleAssignSurvey(survey)}
                            className="text-xs bg-indigo-100 text-indigo-700 px-2 py-1 rounded hover:bg-indigo-200"
                          >
                            Assign Form
                          </button>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span
                          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                            survey.IsActive
                              ? 'bg-green-100 text-green-800'
                              : 'bg-gray-100 text-gray-800'
                          }`}
                        >
                          {survey.IsActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {new Date(survey.CreatedAt).toLocaleDateString()}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                        <button
                          onClick={() => handleEditSurvey(survey.Id)}
                          className="text-blue-600 hover:text-blue-900 mr-3"
                        >
                          Edit
                        </button>
                        <button
                          onClick={() => handleDeleteSurvey(survey.Id)}
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

      {/* Assign Survey Modal */}
      {showAssignModal && selectedSurvey && (
        <AssignSurveyModal
          isOpen={showAssignModal}
          onClose={() => {
            setShowAssignModal(false);
            setSelectedSurvey(null);
          }}
          surveyId={selectedSurvey.Id}
          surveyTitle={selectedSurvey.Title}
          isSelfEvaluation={selectedSurvey.IsSelfEvaluation}
          projectSlug={projectSlug}
          onAssignmentUpdated={handleAssignmentUpdated}
        />
      )}
    </div>
  );
}
