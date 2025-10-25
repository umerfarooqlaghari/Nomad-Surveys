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
        <div>
          <h2 className="text-xl font-semibold text-gray-900">
            {surveyId ? 'Edit Survey' : 'Create New Survey'}
          </h2>
          <p className="text-sm text-gray-600 mt-1">
            Use drag & drop to build your survey. You can include placeholders like{' '}
            <code className="bg-gray-100 px-1 rounded">{'{subjectName}'}</code> or{' '}
            <code className="bg-gray-100 px-1 rounded">{'{evaluatorName}'}</code> in
            question text.
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
      `}</style>
    </div>
  );
}

