/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useEffect, useState } from 'react';
import { SurveyCreatorComponent, SurveyCreator } from 'survey-creator-react';
import 'survey-core/survey-core.min.css';
import 'survey-creator-core/survey-creator-core.min.css';
import toast from 'react-hot-toast';

interface SurveyBuilderProps {
  tenantSlug: string;
  token: string;
  initialSurvey?: any;
  surveyId?: string;
  onSave?: (surveyData: any) => void;
  onCancel?: () => void;
}

export default function SurveyBuilder({
  tenantSlug,
  token,
  initialSurvey,
  surveyId,
  onSave,
  onCancel,
}: SurveyBuilderProps) {
  const [creator, setCreator] = useState<SurveyCreator | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [isSelfEvaluation, setIsSelfEvaluation] = useState(false);

  useEffect(() => {
    const options = {
      showLogicTab: true,
      showTranslationTab: false,
      isAutoSave: false,
    };

    const surveyCreator = new SurveyCreator(options);

    // Set initial survey if provided
    if (initialSurvey) {
      surveyCreator.JSON = initialSurvey.Schema || {};
      setIsSelfEvaluation(initialSurvey.IsSelfEvaluation || false);
    } else {
      // Default empty survey
      surveyCreator.JSON = {
        title: 'New Survey',
        pages: [
          {
            name: 'page1',
            elements: [],
          },
        ],
      };
    }

    // Configure creator settings
    surveyCreator.showToolbox = true;
    surveyCreator.showPropertyGrid = true;

    setCreator(surveyCreator);

    return () => {
      // Cleanup
      surveyCreator.dispose();
    };
  }, [initialSurvey]);

  const handleSave = async () => {
    if (!creator) return;

    try {
      setIsSaving(true);

      const surveyJSON = creator.JSON;
      const surveyTitle = surveyJSON.title || 'Untitled Survey';
      const surveyDescription = surveyJSON.description || '';

      const payload = {
        title: surveyTitle,
        description: surveyDescription,
        schema: surveyJSON,
        isSelfEvaluation: isSelfEvaluation,
      };

      let response;

      if (surveyId) {
        // Update existing survey
        response = await fetch(`/api/${tenantSlug}/surveys/${surveyId}`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify(payload),
        });
      } else {
        // Create new survey
        response = await fetch(`/api/${tenantSlug}/surveys`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify(payload),
        });
      }

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to save survey');
      }

      const savedSurvey = await response.json();

      toast.success(
        surveyId ? 'Survey updated successfully!' : 'Survey created successfully!'
      );

      if (onSave) {
        onSave(savedSurvey);
      }
    } catch (error: any) {
      console.error('Error saving survey:', error);
      toast.error(error.message || 'Failed to save survey');
    } finally {
      setIsSaving(false);
    }
  };

  const handlePreview = () => {
    if (!creator) return;
    creator.showPreview();
  };

  const handleReset = () => {
    if (!creator) return;

    if (
      confirm(
        'Are you sure you want to reset the survey? All unsaved changes will be lost.'
      )
    ) {
      creator.JSON = {
        title: 'New Survey',
        pages: [
          {
            name: 'page1',
            elements: [],
          },
        ],
      };
      toast.success('Survey reset successfully');
    }
  };

  if (!creator) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-gray-500">Loading survey builder...</div>
      </div>
    );
  }

  return (
    <div className="survey-builder-container">
      {/* Action Buttons */}
      <div className="bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center">
        <div className="flex-1">
          <h2 className="text-xl font-semibold text-gray-900">
            {surveyId ? 'Edit Survey' : 'Create New Survey'}
          </h2>

          {/* Survey Type Toggle */}
          <div className="flex items-center gap-6 mt-3 mb-3" style={{ zIndex: 1000, position: 'relative' }}>
            <span className="text-sm font-medium text-gray-700">Survey Type:</span>
            <div className="flex items-center gap-4">
              <div
                className="flex items-center gap-2 cursor-pointer"
                onClick={() => setIsSelfEvaluation(false)}
              >
                <input
                  type="radio"
                  id="radio-360"
                  name="surveyType"
                  value="360"
                  checked={!isSelfEvaluation}
                  onChange={() => setIsSelfEvaluation(false)}
                  className="w-4 h-4 text-blue-600 border-gray-300 focus:ring-blue-500 cursor-pointer"
                  style={{ pointerEvents: 'auto' }}
                />
                <label htmlFor="radio-360" className="text-sm text-gray-700 select-none cursor-pointer">
                  360-Degree Evaluation
                </label>
              </div>
              <div
                className="flex items-center gap-2 cursor-pointer"
                onClick={() => setIsSelfEvaluation(true)}
              >
                <input
                  type="radio"
                  id="radio-self"
                  name="surveyType"
                  value="self"
                  checked={isSelfEvaluation}
                  onChange={() => setIsSelfEvaluation(true)}
                  className="w-4 h-4 text-blue-600 border-gray-300 focus:ring-blue-500 cursor-pointer"
                  style={{ pointerEvents: 'auto' }}
                />
                <label htmlFor="radio-self" className="text-sm text-gray-700 select-none cursor-pointer">
                  Self-Evaluation
                </label>
              </div>
            </div>
          </div>

          {/* Conditional Placeholder Instructions */}
          <p className="text-sm text-gray-600">
            Use drag & drop to build your survey. You can include placeholders like{' '}
            {isSelfEvaluation ? (
              <code className="bg-gray-100 px-1 rounded">{'{employeeName}'}</code>
            ) : (
              <>
                <code className="bg-gray-100 px-1 rounded">{'{subjectName}'}</code> or{' '}
                <code className="bg-gray-100 px-1 rounded">{'{evaluatorName}'}</code>
              </>
            )}{' '}
            in question text.
          </p>
        </div>
        <div className="flex space-x-3">
          {onCancel && (
            <button
              onClick={onCancel}
              className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              Cancel
            </button>
          )}
          <button
            onClick={handleReset}
            className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Reset
          </button>
          <button
            onClick={handlePreview}
            className="px-4 py-2 border border-blue-500 rounded-md text-sm font-medium text-blue-600 bg-white hover:bg-blue-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Preview
          </button>
          <button
            onClick={handleSave}
            disabled={isSaving}
            className="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSaving ? 'Saving...' : surveyId ? 'Update Survey' : 'Save Survey'}
          </button>
        </div>
      </div>

      {/* SurveyJS Creator */}
      <div className="survey-creator-wrapper" style={{ height: 'calc(100vh - 250px)' }}>
        <SurveyCreatorComponent creator={creator} />
      </div>

      <style jsx global>{`
        .survey-builder-container {
          background: white;
          border-radius: 8px;
          overflow: hidden;
          box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.1);
        }

        .survey-creator-wrapper {
          overflow: auto;
        }

        /* Custom styling for SurveyJS Creator */
        .svc-creator {
          height: 100% !important;
        }

        .svc-creator__banner {
          display: none !important;
        }

        code {
          font-family: 'Courier New', monospace;
          font-size: 0.875rem;
        }

        /* SurveyJS Form Theme - Blue and White with Black Text */

        /* Question text - Black */
        .sd-question__title,
        .sd-question__header,
        .sd-element__title,
        .sd-title,
        .sd-page__title,
        .sd-survey__title {
          color: #000000 !important;
        }

        /* Question description - Dark gray */
        .sd-question__description,
        .sd-element__description {
          color: #374151 !important;
        }

        /* Input fields - White background with black text */
        .sd-input,
        .sd-text,
        .sd-comment,
        .sd-dropdown,
        .sd-selectbase,
        input[type="text"],
        input[type="number"],
        input[type="email"],
        textarea,
        select {
          background-color: #ffffff !important;
          color: #000000 !important;
          border: 1px solid #d1d5db !important;
        }

        /* Input focus - Blue border */
        .sd-input:focus,
        .sd-text:focus,
        .sd-comment:focus,
        .sd-dropdown:focus,
        input:focus,
        textarea:focus,
        select:focus {
          border-color: #3b82f6 !important;
          outline: none !important;
          box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1) !important;
        }

        /* Radio buttons and checkboxes - Blue when selected */
        .sd-radio__control,
        .sd-checkbox__control {
          border-color: #d1d5db !important;
        }

        .sd-radio__control:checked,
        .sd-checkbox__control:checked {
          background-color: #3b82f6 !important;
          border-color: #3b82f6 !important;
        }

        /* Radio and checkbox labels - Black text */
        .sd-radio__label,
        .sd-checkbox__label,
        .sd-item__control-label {
          color: #000000 !important;
        }

        /* Buttons - Blue theme */
        .sd-btn,
        .sd-navigation__complete-btn,
        .sd-navigation__next-btn,
        .sd-navigation__prev-btn {
          background-color: #3b82f6 !important;
          color: #ffffff !important;
          border: none !important;
        }

        .sd-btn:hover,
        .sd-navigation__complete-btn:hover,
        .sd-navigation__next-btn:hover {
          background-color: #2563eb !important;
        }

        .sd-navigation__prev-btn {
          background-color: #ffffff !important;
          color: #3b82f6 !important;
          border: 1px solid #3b82f6 !important;
        }

        .sd-navigation__prev-btn:hover {
          background-color: #eff6ff !important;
        }

        /* Page background - White */
        .sd-page,
        .sd-body,
        .sd-container-modern {
          background-color: #ffffff !important;
        }

        /* Panel background - Light gray */
        .sd-panel,
        .sd-question {
          background-color: #f9fafb !important;
          border: 1px solid #e5e7eb !important;
        }

        /* Progress bar - Blue */
        .sd-progress__bar {
          background-color: #3b82f6 !important;
        }

        /* Rating stars - Blue */
        .sd-rating__item--selected {
          color: #3b82f6 !important;
        }

        /* Matrix cells - White background, black text */
        .sd-matrix__cell,
        .sd-table__cell {
          background-color: #ffffff !important;
          color: #000000 !important;
          border-color: #e5e7eb !important;
        }

        /* Dropdown options - Black text */
        .sd-dropdown__item,
        option {
          color: #000000 !important;
          background-color: #ffffff !important;
        }

        .sd-dropdown__item:hover {
          background-color: #eff6ff !important;
        }

        /* Error messages - Red */
        .sd-question__erbox,
        .sd-question--error {
          color: #dc2626 !important;
        }

        /* Placeholder text - Gray */
        .sd-input::placeholder,
        .sd-text::placeholder,
        .sd-comment::placeholder,
        input::placeholder,
        textarea::placeholder {
          color: #9ca3af !important;
        }
      `}</style>
    </div>
  );
}

