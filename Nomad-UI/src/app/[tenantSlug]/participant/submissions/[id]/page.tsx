/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useEffect, useState, use, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import ProtectedRoute from '@/components/ProtectedRoute';
import ParticipantLayout from '@/components/participant/ParticipantLayout';
import { useAuth } from '@/contexts/AuthContext';
import { ArrowLeftIcon, CalendarIcon } from '@heroicons/react/24/outline';
import { SurveySchema } from '@/types/survey';
import QuestionRenderer from '@/components/survey/QuestionRenderer';
import { toTitleCase } from '@/lib/stringUtils';

interface SubmissionDetailProps {
  params: Promise<{ tenantSlug: string; id: string }>;
}

export default function SubmissionDetail({ params }: SubmissionDetailProps) {
  const resolvedParams = use(params);
  const tenantSlug = resolvedParams.tenantSlug;
  const submissionId = resolvedParams.id;
  const router = useRouter();
  const { token } = useAuth();
  const [surveySchema, setSurveySchema] = useState<SurveySchema | null>(null);
  const [responseData, setResponseData] = useState<Record<string, any>>({});
  const [relationshipType, setRelationshipType] = useState<string>('Self');
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
        CompletedDate: data.CompletedAt || data.SubmittedAt,
      });

      // Parse and transform survey schema to apply Title Case to embedded names
      let schemaString = typeof data.SurveySchema === 'string'
        ? data.SurveySchema
        : JSON.stringify(data.SurveySchema);

      // replace raw names with Title Case versions in the schema text
      const subjectTitleCase = toTitleCase(data.SubjectName);
      const evaluatorTitleCase = toTitleCase(data.EvaluatorName);

      if (data.SubjectName && data.SubjectName !== subjectTitleCase) {
        schemaString = schemaString.split(data.SubjectName).join(subjectTitleCase);
      }
      if (data.EvaluatorName && data.EvaluatorName !== evaluatorTitleCase) {
        schemaString = schemaString.split(data.EvaluatorName).join(evaluatorTitleCase);
      }

      const schema: SurveySchema = JSON.parse(schemaString);

      setSurveySchema(schema);
      setRelationshipType(data.RelationshipType || 'Self');

      // Parse response data
      const responses = typeof data.ResponseData === 'string'
        ? JSON.parse(data.ResponseData)
        : data.ResponseData || {};

      setResponseData(responses);
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
                    Subject: <span className="font-semibold">{toTitleCase(submissionData.SubjectName)}</span>
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
            ) : surveySchema ? (
              <div className="space-y-8">
                {surveySchema.pages.map((page, pageIndex) => {
                  const isSelf = relationshipType === 'Self';

                  // Filter questions based on visibility
                  const visibleQuestions = page.questions.filter((q) => {
                    if (!q.showTo || q.showTo === 'everyone') return true;
                    if (q.showTo === 'self' && isSelf) return true;
                    if (q.showTo === 'others' && !isSelf) return true;
                    return false;
                  });

                  if (visibleQuestions.length === 0) return null;

                  return (
                    <div key={page.id} className="space-y-6">
                      {/* Page Header */}
                      {surveySchema.pages.length > 1 && (
                        <div className="border-b border-gray-200 pb-4">
                          <h2 className="text-xl font-semibold text-gray-900">
                            Page {pageIndex + 1}
                            {page.title && `: ${page.title}`}
                          </h2>
                          {page.description && (
                            <p className="text-sm text-gray-600 mt-1">{page.description}</p>
                          )}
                        </div>
                      )}

                      {/* Questions */}
                      <div className="space-y-6">
                        <div className="text-black">
                          {visibleQuestions.map((question, index) => (
                            <QuestionRenderer
                              key={question.id}
                              question={question}
                              questionNumber={index + 1}
                              isSelf={isSelf}
                              value={responseData[question.id]}
                              onChange={() => { }} // Read-only, no changes allowed
                              isPreview={true} // Set to preview mode (read-only)
                            />
                          ))}
                        </div>
                      </div>
                    </div>
                  );
                })}
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
      </ParticipantLayout>
    </ProtectedRoute>
  );
}

