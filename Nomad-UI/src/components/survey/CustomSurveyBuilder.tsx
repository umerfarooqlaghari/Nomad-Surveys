/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useCallback } from 'react';
import toast from 'react-hot-toast';
import {
  SurveySchema,
  SurveyPage,
  Question,
  createDefaultSurvey,
  createDefaultPage,
  validateSurvey,
  generatePageId,
} from '@/types/survey';
import PageEditor from './PageEditor';
import PreviewModal from './PreviewModal';

interface CustomSurveyBuilderProps {
  tenantSlug: string;
  token: string;
  initialSurvey?: any;
  surveyId?: string;
  onSave?: (surveyData: any) => void;
  onCancel?: () => void;
}

export default function CustomSurveyBuilder({
  tenantSlug,
  token,
  initialSurvey,
  surveyId,
  onSave,
  onCancel,
}: CustomSurveyBuilderProps) {
  const [survey, setSurvey] = useState<SurveySchema>(() => {
    if (initialSurvey?.Schema) {
      // Load existing survey from backend schema
      return initialSurvey.Schema as SurveySchema;
    }
    return createDefaultSurvey();
  });

  const [isSaving, setIsSaving] = useState(false);
  const [showPreview, setShowPreview] = useState(false);
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);
  const [autoAssign, setAutoAssign] = useState<boolean>(true);
  const [activeTab, setActiveTab] = useState<'form' | 'json'>('form');
  const [jsonInput, setJsonInput] = useState('');

  // Update survey title
  const handleTitleChange = useCallback((title: string) => {
    setSurvey((prev) => ({ ...prev, title }));
    setHasUnsavedChanges(true);
  }, []);

  // Update survey description
  const handleDescriptionChange = useCallback((description: string) => {
    setSurvey((prev) => ({ ...prev, description }));
    setHasUnsavedChanges(true);
  }, []);

  // Add a new page
  const handleAddPage = useCallback(() => {
    const newPage = createDefaultPage();
    newPage.title = `Page ${survey.pages.length + 1}`;
    newPage.order = survey.pages.length;

    setSurvey((prev) => ({
      ...prev,
      pages: [...prev.pages, newPage],
    }));
    setHasUnsavedChanges(true);
    toast.success('Page added');
  }, [survey.pages.length]);

  // Update a page
  const handleUpdatePage = useCallback((pageId: string, updatedPage: SurveyPage) => {
    setSurvey((prev) => ({
      ...prev,
      pages: prev.pages.map((p) => (p.id === pageId ? updatedPage : p)),
    }));
    setHasUnsavedChanges(true);
  }, []);

  // Delete a page
  const handleDeletePage = useCallback((pageId: string) => {
    if (survey.pages.length === 1) {
      toast.error('Cannot delete the last page');
      return;
    }

    if (!confirm('Are you sure you want to delete this page? All questions in this page will be lost.')) {
      return;
    }

    setSurvey((prev) => ({
      ...prev,
      pages: prev.pages
        .filter((p) => p.id !== pageId)
        .map((p, index) => ({ ...p, order: index })),
    }));
    setHasUnsavedChanges(true);
    toast.success('Page deleted');
  }, [survey.pages.length]);

  // Reorder pages
  const handleReorderPages = useCallback((reorderedPages: SurveyPage[]) => {
    setSurvey((prev) => ({
      ...prev,
      pages: reorderedPages.map((p, index) => ({ ...p, order: index })),
    }));
    setHasUnsavedChanges(true);
  }, []);

  // Save survey
  const handleSave = async () => {
    // Validate survey
    const errors = validateSurvey(survey);
    if (errors.length > 0) {
      toast.error(`Validation failed: ${errors[0].message}`);
      console.error('Validation errors:', errors);
      return;
    }

    try {
      setIsSaving(true);

      const payload = {
        title: survey.title,
        description: survey.description || '',
        schema: survey,
        ...(surveyId ? {} : { autoAssign }), // Only include autoAssign for new surveys
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

      setHasUnsavedChanges(false);

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

  // Handle cancel
  const handleCancelClick = () => {
    if (hasUnsavedChanges) {
      if (!confirm('You have unsaved changes. Are you sure you want to cancel?')) {
        return;
      }
    }

    if (onCancel) {
      onCancel();
    }
  };

  // Reset survey
  const handleReset = () => {
    if (!confirm('Are you sure you want to reset the survey? All changes will be lost.')) {
      return;
    }

    if (initialSurvey?.Schema) {
      setSurvey(initialSurvey.Schema as SurveySchema);
    } else {
      setSurvey(createDefaultSurvey());
    }
    setHasUnsavedChanges(false);
    toast.success('Survey reset');
  };

  // Export survey as JSON file
  const handleExportJson = () => {
    const jsonString = JSON.stringify(survey, null, 2);
    const blob = new Blob([jsonString], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${survey.title || 'survey'}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
    toast.success('Survey exported as JSON');
  };

  // Switch to JSON tab
  const handleSwitchToJsonTab = () => {
    setJsonInput(JSON.stringify(survey, null, 2));
    setActiveTab('json');
  };

  // Apply JSON changes to survey
  const handleApplyJson = () => {
    try {
      const parsed = JSON.parse(jsonInput);

      // Basic validation
      if (!parsed.title || !parsed.pages || !Array.isArray(parsed.pages)) {
        throw new Error('Invalid survey JSON structure. Must have "title" and "pages" array.');
      }

      setSurvey(parsed as SurveySchema);
      setHasUnsavedChanges(true);
      setActiveTab('form');
      toast.success('Survey updated from JSON');
    } catch (error: any) {
      toast.error(`Invalid JSON: ${error.message}`);
    }
  };

  return (
    <div className="w-full h-full bg-gray-50">
      {/* Top Action Bar */}
      <div className="bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center sticky top-0 z-10">
        <div className="flex-1">
          <h2 className="text-xl font-semibold text-gray-900">
            {surveyId ? 'Edit Survey' : 'Create New Survey'}
          </h2>
          {hasUnsavedChanges && (
            <p className="text-sm text-orange-600 mt-1">You have unsaved changes</p>
          )}
        </div>

        <div className="flex gap-3">
          <button
            onClick={handleCancelClick}
            className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleReset}
            className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
          >
            Reset
          </button>
          <button
            onClick={handleExportJson}
            className="px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            title="Export survey as JSON file"
          >
             Export JSON
          </button>
          <button
            onClick={() => setShowPreview(true)}
            className="px-4 py-2 text-blue-700 bg-blue-50 border border-blue-300 rounded-lg hover:bg-blue-100 transition-colors"
          >
            Preview
          </button>
          <button
            onClick={handleSave}
            disabled={isSaving}
            className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSaving ? 'Saving...' : surveyId ? 'Update Survey' : 'Save Survey'}
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-5xl mx-auto px-6">
          <div className="flex gap-1">
            <button
              onClick={() => {
                if (activeTab === 'json') {
                  // Sync JSON to form before switching
                  try {
                    const parsed = JSON.parse(jsonInput);
                    if (parsed.title && parsed.pages && Array.isArray(parsed.pages)) {
                      setSurvey(parsed as SurveySchema);
                      setHasUnsavedChanges(true);
                    }
                  } catch (error) {
                    // Ignore JSON errors when switching tabs
                  }
                }
                setActiveTab('form');
              }}
              className={`px-6 py-3 font-medium border-b-2 transition-colors ${
                activeTab === 'form'
                  ? 'border-blue-600 text-blue-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
               Form Editor
            </button>
            <button
              onClick={handleSwitchToJsonTab}
              className={`px-6 py-3 font-medium border-b-2 transition-colors ${
                activeTab === 'json'
                  ? 'border-blue-600 text-blue-600'
                  : 'border-transparent text-gray-600 hover:text-gray-900'
              }`}
            >
               JSON Editor
            </button>
          </div>
        </div>
      </div>

      {/* Survey Builder Content */}
      <div className="max-w-5xl mx-auto p-6">
        {activeTab === 'form' ? (
          <>
            {/* Survey Title and Description */}
            <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
              <div className="mb-4">
                <label className="block text-sm font-medium text-black mb-2">
                  Survey Title *
                </label>
                <input
                  type="text"
                  value={survey.title}
                  onChange={(e) => handleTitleChange(e.target.value)}
                  placeholder="Enter survey title..."
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-400"
                />
              </div>

              <div className="mb-4">
                <label className="block text-sm font-medium text-black mb-2">
                  Description (Optional)
                </label>
                <textarea
                  value={survey.description || ''}
                  onChange={(e) => handleDescriptionChange(e.target.value)}
                  placeholder="Enter survey description..."
                  rows={3}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-400"
                />
              </div>

              {/* Auto-Assign Option - Only show when creating new survey */}
              {!surveyId && (
                <div className="pt-4 border-t border-gray-200">
                  <label className="block text-sm font-medium text-black mb-3">
                    Evaluator Assignment <span className="text-red-500">*</span>
                  </label>
                  <div className="space-y-2">
                    <label
                      className="flex items-center cursor-pointer"
                      onClick={() => setAutoAssign(false)}
                    >
                      <input
                        type="radio"
                        name="assignmentType"
                        value="manual"
                        checked={!autoAssign}
                        // onChange={() => {}}
                        // onClick={(e) => e.stopPropagation()}
                        onChange={() => setAutoAssign(false)} // ✅ handle inside onChange
                        className="h-4 w-4 text-blue-600 border-gray-300 focus:ring-blue-500 cursor-pointer"
                      />
                      <span className="ml-3 text-sm text-gray-700">
                        Assign Evaluators Manually
                      </span>
                    </label>
                    <label
                      className="flex items-center cursor-pointer"
                      onClick={() => setAutoAssign(true)}
                    >
                      <input
                        type="radio"
                        name="assignmentType"
                        value="auto"
                        checked={autoAssign}
                        // onChange={() => {}}
                        // onClick={(e) => e.stopPropagation()}
                        onChange={() => setAutoAssign(true)} // ✅ handle inside onChange
                        className="h-4 w-4 text-blue-600 border-gray-300 focus:ring-blue-500 cursor-pointer"
                      />
                      <span className="ml-3 text-sm text-gray-700">
                        Auto-Assign to All Evaluators
                      </span>
                    </label>
                  </div>
                  <p className="mt-2 text-xs text-gray-500">
                    {autoAssign
                      ? 'The survey will be automatically assigned to all existing subject-evaluator relationships.'
                      : 'You can manually assign the survey to specific evaluators after creation.'}
                  </p>
                </div>
              )}
            </div>

            {/* Pages */}
        {survey.pages.map((page, index) => (
          <PageEditor
            key={page.id}
            page={page}
            pageNumber={index + 1}
            totalPages={survey.pages.length}
            tenantSlug={tenantSlug}
            token={token}
            onUpdate={(updatedPage: SurveyPage) => handleUpdatePage(page.id, updatedPage)}
            onDelete={() => handleDeletePage(page.id)}
            onMoveUp={
              index > 0
                ? () => {
                    const newPages = [...survey.pages];
                    [newPages[index - 1], newPages[index]] = [newPages[index], newPages[index - 1]];
                    handleReorderPages(newPages);
                  }
                : undefined
            }
            onMoveDown={
              index < survey.pages.length - 1
                ? () => {
                    const newPages = [...survey.pages];
                    [newPages[index], newPages[index + 1]] = [newPages[index + 1], newPages[index]];
                    handleReorderPages(newPages);
                  }
                : undefined
            }
          />
        ))}

            {/* Add Page Button */}
            <button
              onClick={handleAddPage}
              className="w-full py-3 text-black border-2 border-dashed border-gray-300 rounded-lg text-gray-600 hover:border-blue-500 hover:text-blue-600 transition-colors"
            >
              + Add Page
            </button>
          </>
        ) : (
          /* JSON Editor Tab */
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="mb-4">
              <div className="flex items-center justify-between mb-2">
                <h3 className="text-lg font-semibold text-black">JSON Editor</h3>
                <button
                  onClick={handleApplyJson}
                  className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Apply Changes
                </button>
              </div>
              <p className="text-sm text-gray-600 mb-4">
                Edit the survey JSON directly. Click &quot;Apply Changes&quot; to update the form, or switch to &quot;Form Editor&quot; tab to see the visual representation.
              </p>
            </div>

            <textarea
              value={jsonInput}
              onChange={(e) => setJsonInput(e.target.value)}
              className="w-full h-[calc(100vh-300px)] px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono text-sm text-black bg-gray-50"
              placeholder="Paste your survey JSON here..."
              spellCheck={false}
            />

            <div className="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
              <h4 className="text-sm font-semibold text-blue-900 mb-2">💡 Tips:</h4>
              <ul className="text-xs text-blue-800 space-y-1">
                <li>• Edit the JSON directly in the editor above</li>
                <li>• Click &quot;Apply Changes&quot; to validate and update the form</li>
                <li>• Switch to &quot;Form Editor&quot; tab to see the visual representation</li>
                <li>• Use &quot;Export JSON&quot; button to download as a file</li>
                <li>• The JSON must have &quot;title&quot; and &quot;pages&quot; array to be valid</li>
              </ul>
            </div>
          </div>
        )}
      </div>

      {/* Preview Modal */}
      {showPreview && (
        <PreviewModal
          survey={survey}
          onClose={() => setShowPreview(false)}
        />
      )}
    </div>
  );
}

