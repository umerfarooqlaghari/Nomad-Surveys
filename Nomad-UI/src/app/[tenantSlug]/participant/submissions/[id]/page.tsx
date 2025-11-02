'use client';

import React, { useEffect, useState, use, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import ProtectedRoute from '@/components/ProtectedRoute';
import ParticipantLayout from '@/components/participant/ParticipantLayout';
import { useAuth } from '@/contexts/AuthContext';
import { Model } from 'survey-core';
import { Survey } from 'survey-react-ui';
import 'survey-core/defaultV2.min.css';
import { ArrowLeftIcon, CalendarIcon } from '@heroicons/react/24/outline';

interface SubmissionDetailProps {
  params: Promise<{ tenantSlug: string; id: string }>;
}

export default function SubmissionDetail({ params }: SubmissionDetailProps) {
  const resolvedParams = use(params);
  const tenantSlug = resolvedParams.tenantSlug;
  const submissionId = resolvedParams.id;
  const router = useRouter();
  const { token } = useAuth();
  const [surveyModel, setSurveyModel] = useState<Model | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [submissionData, setSubmissionData] = useState<{
    SubjectName: string;
    SurveyTitle: string;
    CompletedDate: string;
  } | null>(null);

  const loadSubmission = useCallback(async () => {
    try {
      setIsLoading(true);

      // Fetch submission data from API
      console.log('Fetching submission from:', `/api/${tenantSlug}/participant/submissions/${submissionId}`);
      const response = await fetch(`/api/${tenantSlug}/participant/submissions/${submissionId}`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Submission API error:', response.status, errorText);
        throw new Error(`Failed to fetch submission: ${response.status}`);
      }

      const data = await response.json();
      console.log('Submission data:', data);

      setSubmissionData({
        SubjectName: data.SubjectName,
        SurveyTitle: data.SurveyTitle,
        CompletedDate: data.CompletedDate,
      });

      // Create survey model with the schema
      const survey = new Model(data.SurveySchema);

      // Load the submitted response data
      if (data.ResponseData) {
        survey.data = data.ResponseData;
      }

      // Set to read-only mode
      survey.mode = 'display';

      // Apply custom theme
      survey.applyTheme({
        isPanelless: true,
        cssVariables: {
          '--sjs-primary-backcolor': 'rgba(2, 132, 199, 1)',
          '--sjs-secondary-backcolor': 'rgba(224, 242, 254, 1)',
          '--sjs-general-backcolor': 'rgba(255, 255, 255, 1)',
          '--sjs-general-forecolor': 'rgba(0, 0, 0, 1)',
          '--sjs-font-editorfont-color': 'rgba(0, 0, 0, 1)',
          '--sjs-corner-radius': '0.5rem',
        },
        themeName: 'participant-survey-readonly',
        colorPalette: 'light',
      });

      setSurveyModel(survey);
    } catch (error) {
      console.error('Error loading submission:', error);
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug, submissionId, token]);

  useEffect(() => {
    if (token) {
      loadSubmission();
    }
  }, [token, loadSubmission]);

  return (
    <ProtectedRoute allowedRoles={['Participant']}>
      <ParticipantLayout>
        <div className="max-w-4xl mx-auto">
          {/* Header */}
          <div className="mb-6">
            <button
              onClick={() => router.push(`/${tenantSlug}/participant/submissions`)}
              className="flex items-center text-sm font-medium text-blue-600 hover:text-blue-700 mb-4"
            >
              <ArrowLeftIcon className="h-4 w-4 mr-1" />
              Back to Submissions
            </button>
            {submissionData && (
              <>
                <h1 className="text-2xl font-bold text-black">{submissionData.SurveyTitle}</h1>
                <div className="flex items-center gap-4 mt-2">
                  <p className="text-sm text-black">
                    Subject: <span className="font-semibold">{submissionData.SubjectName}</span>
                  </p>
                  <div className="flex items-center text-sm text-black">
                    <CalendarIcon className="h-4 w-4 mr-1 text-gray-400" />
                    Submitted: {submissionData.CompletedDate
                      ? new Date(submissionData.CompletedDate).toLocaleDateString('en-US', {
                          year: 'numeric',
                          month: 'short',
                          day: 'numeric'
                        })
                      : 'N/A'}
                  </div>
                </div>
              </>
            )}
          </div>

          {/* Submission Summary */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            {isLoading ? (
              <div className="py-12 text-center">
                <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-blue-600 border-r-transparent"></div>
                <p className="text-sm text-black mt-4">Loading submission...</p>
              </div>
            ) : surveyModel ? (
              <div className="survey-container">
                <Survey model={surveyModel} />
              </div>
            ) : (
              <div className="py-12 text-center">
                <p className="text-sm font-medium text-black">Failed to load submission</p>
                <button
                  onClick={loadSubmission}
                  className="mt-4 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Retry
                </button>
              </div>
            )}
          </div>
        </div>

        <style jsx global>{`
          .survey-container .sd-root-modern {
            background-color: transparent !important;
          }
          .survey-container .sd-root-modern *,
          .survey-container .sd-question,
          .survey-container .sd-question *,
          .survey-container .sd-title,
          .survey-container .sd-description,
          .survey-container label,
          .survey-container span,
          .survey-container p,
          .survey-container div {
            color: #000000 !important;
          }
          .survey-container .sd-input {
            background-color: #f9fafb !important;
            border-color: #e5e7eb !important;
          }
        `}</style>
      </ParticipantLayout>
    </ProtectedRoute>
  );
}

