/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useEffect, useState, use, useRef, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import ProtectedRoute from '@/components/ProtectedRoute';
import ParticipantLayout from '@/components/participant/ParticipantLayout';
import { useAuth } from '@/contexts/AuthContext';
import { Model } from 'survey-core';
import { Survey } from 'survey-react-ui';
import 'survey-core/defaultV2.min.css';
import { ArrowLeftIcon, CheckCircleIcon } from '@heroicons/react/24/outline';

interface EvaluationFormProps {
  params: Promise<{ tenantSlug: string; id: string }>;
}

// Step 1: Memoized Survey wrapper that never re-renders once mounted
const SurveyWrapper = React.memo(({ model }: { model: Model }) => {
  console.log('SurveyWrapper render - this should only happen ONCE');
  return <Survey model={model} />;
});
SurveyWrapper.displayName = 'SurveyWrapper';

// Step 2: Memoized Header component to isolate from Survey re-renders
const Header = React.memo(({
  evaluationData,
  onBack
}: {
  evaluationData: { SubjectName: string; SurveyTitle: string } | null;
  onBack: () => void;
}) => {
  return (
    <div className="mb-6">
      <button
        onClick={onBack}
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
          </p>
        </>
      )}
    </div>
  );
});
Header.displayName = 'Header';

// Step 2: Memoized Survey section to isolate rendering
const SurveySection = React.memo(({
  isLoading,
  survey
}: {
  isLoading: boolean;
  survey: Model | null;
}) => {
  console.log('SurveySection render - isLoading:', isLoading, 'survey:', !!survey);

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      {isLoading ? (
        <div className="py-12 text-center">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-blue-600 border-r-transparent"></div>
          <p className="text-sm text-black mt-4">Loading evaluation form...</p>
        </div>
      ) : survey ? (
        <div className="survey-container">
          <SurveyWrapper model={survey} />
        </div>
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
  );
});
SurveySection.displayName = 'SurveySection';

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
  } | null>(null);
  const [surveySchema, setSurveySchema] = useState<any>(null);
  const [savedData, setSavedData] = useState<any>(null);

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
          });

          // Store schema and saved data in state
          setSurveySchema(data.SurveySchema);
          setSavedData(data.SavedResponseData || {});

        } catch (error) {
          console.error('Error loading evaluation:', error);
        } finally {
          setIsLoading(false);
        }
      };

      loadEvaluation();
    }
  }, [user, tenantSlug, assignmentId, token]);

  // Use useMemo to create the survey model when schema is available
  // This ensures the model is created only once when schema changes from null to actual data
  const survey = useMemo(() => {
    if (!surveySchema) {
      console.log('No schema yet, returning null');
      return null;
    }

    console.log('Creating survey model - this should only happen ONCE when schema is loaded');
    const model = new Model(surveySchema);

    // Step 3: Load saved data only if model is empty (prevent overwriting user input)
    if (savedData && Object.keys(savedData).length > 0 && Object.keys(model.data).length === 0) {
      model.data = savedData;
      console.log('Loaded saved data into survey model:', savedData);
    }

    // Apply custom theme
    model.applyTheme({
      isPanelless: true,
      cssVariables: {
        '--sjs-primary-backcolor': 'rgba(2, 132, 199, 1)',
        '--sjs-secondary-backcolor': 'rgba(224, 242, 254, 1)',
        '--sjs-general-backcolor': 'rgba(255, 255, 255, 1)',
        '--sjs-general-forecolor': 'rgba(0, 0, 0, 1)',
        '--sjs-font-editorfont-color': 'rgba(0, 0, 0, 1)',
        '--sjs-corner-radius': '0.5rem',
      },
      themeName: 'participant-survey',
      colorPalette: 'light',
    });

    return model;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [surveySchema]); // Only recreate when schema changes (happens only once). savedData is intentionally not included to prevent overwriting user input.

  // Setup event handlers using useEffect - runs only when survey is created
  useEffect(() => {
    if (!survey) return;

    console.log('Setting up survey event handlers');

    // Auto-save on value change
    const onValueChangedHandler = () => {
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
              ResponseData: survey.data,
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

    // Handle survey completion
    const onCompleteHandler = async (sender: Model) => {
      try {
        console.log('Submitting evaluation...');

        const submitResponse = await fetch(`/api/${tenantSlug}/participant/evaluations/${assignmentId}/submit`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`,
          },
          body: JSON.stringify({
            ResponseData: sender.data,
          }),
        });

        if (!submitResponse.ok) {
          const errorText = await submitResponse.text();
          console.error('Submit API error:', submitResponse.status, errorText);
          throw new Error(`Failed to submit evaluation: ${submitResponse.status}`);
        }

        setIsSubmitted(true);
      } catch (error) {
        console.error('Error submitting evaluation:', error);
      }
    };

    survey.onValueChanged.add(onValueChangedHandler);
    survey.onComplete.add(onCompleteHandler);

    // Cleanup event handlers on unmount
    return () => {
      survey.onValueChanged.remove(onValueChangedHandler);
      survey.onComplete.remove(onCompleteHandler);
    };
  }, [survey, tenantSlug, assignmentId, token]);

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
          {/* Step 2: Isolated Header component */}
          <Header
            evaluationData={evaluationData}
            onBack={() => router.push(`/${tenantSlug}/participant/evaluations`)}
          />

          {/* Step 2: Isolated Survey section */}
          <SurveySection
            isLoading={isLoading}
            survey={survey}
          />
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
          .survey-container .sd-btn--action {
            background-color: #0284c7 !important;
            color: #ffffff !important;
          }
          .survey-container .sd-btn--action:hover {
            background-color: #0369a1 !important;
          }
        `}</style>
      </ParticipantLayout>
    </ProtectedRoute>
  );
}

