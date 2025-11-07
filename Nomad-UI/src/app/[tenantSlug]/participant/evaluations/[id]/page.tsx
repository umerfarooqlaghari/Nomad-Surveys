/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useEffect, useState, use, useRef } from 'react';
import { useRouter } from 'next/navigation';
import ProtectedRoute from '@/components/ProtectedRoute';
import ParticipantLayout from '@/components/participant/ParticipantLayout';
import { useAuth } from '@/contexts/AuthContext';
import { ArrowLeftIcon, CheckCircleIcon } from '@heroicons/react/24/outline';
import SurveyRenderer from '@/components/survey/SurveyRenderer';
import { SurveySchema } from '@/types/survey';
import toast from 'react-hot-toast';

interface EvaluationFormProps {
  params: Promise<{ tenantSlug: string; id: string }>;
}

export default function EvaluationForm({ params }: EvaluationFormProps) {
  const resolvedParams = use(params);
  const tenantSlug = resolvedParams.tenantSlug;
  const assignmentId = resolvedParams.id;
  const router = useRouter();
  const { user, token } = useAuth();

  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [evaluationData, setEvaluationData] = useState<{
    SubjectName: string;
    SurveyTitle: string;
    RelationshipType: string;
  } | null>(null);
  const [surveySchema, setSurveySchema] = useState<SurveySchema | null>(null);
  const [responses, setResponses] = useState<Record<string, any>>({});

  const autoSaveTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const hasLoadedRef = useRef(false);

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (autoSaveTimeoutRef.current) {
        clearTimeout(autoSaveTimeoutRef.current);
      }
    };
  }, []);

  // Load evaluation data once on mount
  useEffect(() => {
    if (user && tenantSlug && !hasLoadedRef.current) {
      hasLoadedRef.current = true;

      const loadEvaluation = async () => {
        try {
          setIsLoading(true);

          console.log('Fetching evaluation form from:', `/api/${tenantSlug}/participant/evaluations/${assignmentId}`);
          const response = await fetch(`/api/${tenantSlug}/participant/evaluations/${assignmentId}`, {
            headers: {
              'Authorization': `Bearer ${token}`,
            },
          });

          if (!response.ok) {
            const errorText = await response.text();
            console.error('Evaluation form API error:', response.status, errorText);
            throw new Error(`Failed to fetch evaluation form: ${response.status}`);
          }

          const data = await response.json();
          console.log('Evaluation form data:', data);

          setEvaluationData({
            SubjectName: data.SubjectName,
            SurveyTitle: data.SurveyTitle,
            RelationshipType: data.RelationshipType || '',
          });

          // Store schema and saved data in state
          setSurveySchema(data.SurveySchema);
          setResponses(data.SavedResponseData || {});

        } catch (error) {
          console.error('Error loading evaluation:', error);
        } finally {
          setIsLoading(false);
        }
      };

      loadEvaluation();
    }
  }, [user, tenantSlug, assignmentId, token]);

  // Handle data changes with auto-save
  const handleDataChange = (data: Record<string, any>) => {
    setResponses(data);

    // Clear any existing timeout
    if (autoSaveTimeoutRef.current) {
      clearTimeout(autoSaveTimeoutRef.current);
    }

    // Debounce auto-save by 1 second
    autoSaveTimeoutRef.current = setTimeout(async () => {
      try {
        console.log('Auto-saving draft...');

        const saveResponse = await fetch(`/api/${tenantSlug}/participant/evaluations/${assignmentId}/save-draft`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`,
          },
          body: JSON.stringify({
            ResponseData: data,
          }),
        });

        if (!saveResponse.ok) {
          const errorText = await saveResponse.text();
          console.error('Auto-save API error:', saveResponse.status, errorText);
        } else {
          console.log('Draft saved successfully');
        }
      } catch (error) {
        console.error('Error auto-saving:', error);
      }
    }, 1000);
  };

  // Handle survey submission
  const handleSubmit = async () => {
    try {
      console.log('Submitting evaluation...');

      const submitResponse = await fetch(`/api/${tenantSlug}/participant/evaluations/${assignmentId}/submit`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({
          ResponseData: responses,
        }),
      });

      if (!submitResponse.ok) {
        const errorText = await submitResponse.text();
        console.error('Submit API error:', submitResponse.status, errorText);
        toast.error('Failed to submit evaluation');
        return;
      }

      toast.success('Evaluation submitted successfully!');
      setIsSubmitted(true);
    } catch (error) {
      console.error('Error submitting evaluation:', error);
      toast.error('Failed to submit evaluation');
    }
  };

  if (isSubmitted) {
    return (
      <ProtectedRoute allowedRoles={['Participant']}>
        <ParticipantLayout>
          <div className="max-w-3xl mx-auto">
            <div className="bg-white rounded-lg border border-gray-200 p-12 text-center">
              <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-green-100 mb-6">
                <CheckCircleIcon className="h-10 w-10 text-green-600" />
              </div>
              <h1 className="text-2xl font-bold text-black mb-2">Thank You!</h1>
              <p className="text-black mb-8">
                Your evaluation has been submitted successfully.
              </p>
              <div className="flex justify-center gap-4">
                <button
                  onClick={() => router.push(`/${tenantSlug}/participant/evaluations`)}
                  className="px-6 py-2 border border-gray-300 text-black rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Back to Evaluations
                </button>
                <button
                  onClick={() => router.push(`/${tenantSlug}/participant/dashboard`)}
                  className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Go to Dashboard
                </button>
              </div>
            </div>
          </div>
        </ParticipantLayout>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute allowedRoles={['Participant']}>
      <ParticipantLayout>
        <div className="max-w-4xl mx-auto">
          {/* Header */}
          <div className="mb-6">
            <button
              onClick={() => router.push(`/${tenantSlug}/participant/evaluations`)}
              className="flex items-center text-sm font-medium text-blue-600 hover:text-blue-700 mb-4"
            >
              <ArrowLeftIcon className="h-4 w-4 mr-1" />
              Back to Evaluations
            </button>
            {evaluationData && (
              <>
                <h1 className="text-2xl font-bold text-black">{evaluationData.SurveyTitle}</h1>
                <p className="text-sm text-black mt-1">
                  Evaluating: <span className="font-semibold">{evaluationData.SubjectName}</span>
                  {evaluationData.RelationshipType && (
                    <span className="ml-2 text-gray-600">
                      ({evaluationData.RelationshipType})
                    </span>
                  )}
                </p>
              </>
            )}
          </div>

          {/* Survey Section */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            {isLoading ? (
              <div className="py-12 text-center">
                <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-blue-600 border-r-transparent"></div>
                <p className="text-sm text-black mt-4">Loading evaluation form...</p>
              </div>
            ) : surveySchema && evaluationData ? (
              <SurveyRenderer
                survey={surveySchema}
                relationshipType={evaluationData.RelationshipType}
                initialData={responses}
                onDataChange={handleDataChange}
                onSubmit={handleSubmit}
                isPreview={false}
                showHeader={false}
              />
            ) : (
              <div className="py-12 text-center">
                <p className="text-sm font-medium text-black">Failed to load evaluation form</p>
                <button
                  onClick={() => window.location.reload()}
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

