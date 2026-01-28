/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useCallback, useEffect } from 'react';
import toast from 'react-hot-toast';
import {
  SurveySchema,
  SurveyPage,
  Question,
  createDefaultSurvey,
  createDefaultPage,
  validateSurvey,
  generatePageId,
  TenantSettings,
  DEFAULT_TENANT_SETTINGS,
} from '@/types/survey';
import PageEditor from './PageEditor';
import PreviewModal from './PreviewModal';
import SurveySettingsTab from './SurveySettingsTab';

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
  const [autoAssign, setAutoAssign] = useState<boolean>(false);
  const [activeTab, setActiveTab] = useState<'form' | 'settings' | 'json'>('form');
  const [jsonInput, setJsonInput] = useState('');
  const [tenantSettings, setTenantSettings] = useState<TenantSettings | null>(null);

  // Load tenant settings on mount
  useEffect(() => {
    if (token) {
      loadTenantSettings();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  const loadTenantSettings = async () => {
    try {
      // Try to get token from props or localStorage
      const authToken = token || localStorage.getItem('token');
      console.log('🔑 Loading tenant settings with token:', authToken ? 'Present' : 'Missing');

      if (!authToken) {
        console.warn('⚠️ No token available, using default settings');
        setTenantSettings({
          ...DEFAULT_TENANT_SETTINGS,
        });
        return;
      }

      const response = await fetch(`/api/${tenantSlug}/settings`, {
        headers: {
          'Authorization': `Bearer ${authToken}`,
        },
      });

      console.log('📡 Settings API response:', response.status, response.statusText);

      if (response.ok) {
        const data = await response.json();
        console.log('📦 Settings data (raw from API):', data);

        // If data is null, settings don't exist yet - use defaults
        if (data === null) {
          console.log('⚠️ No settings found, using defaults');
          setTenantSettings({
            ...DEFAULT_TENANT_SETTINGS,
          });
        } else {
          // Transform PascalCase to camelCase
          const transformedSettings: TenantSettings = {
            id: data.Id,
            tenantId: data.TenantId,
            defaultQuestionType: data.DefaultQuestionType || DEFAULT_TENANT_SETTINGS.defaultQuestionType,
            defaultRatingOptions: data.DefaultRatingOptions?.map((opt: any) => ({
              id: opt.Id || opt.id,
              text: opt.Text || opt.text,
              order: opt.Order ?? opt.order,
            })) || DEFAULT_TENANT_SETTINGS.defaultRatingOptions,
            numberOfOptions: data.NumberOfOptions ?? DEFAULT_TENANT_SETTINGS.numberOfOptions,
          };
          console.log('✅ Settings loaded and transformed:', transformedSettings);
          console.log('⚠️ Missing fields check:', {
            hasDefaultRatingOptions: !!data.DefaultRatingOptions,
            hasNumberOfOptions: data.NumberOfOptions !== undefined && data.NumberOfOptions !== null,
            rawData: data,
          });
          setTenantSettings(transformedSettings);
        }
      } else {
        const errorData = await response.json().catch(() => ({ error: 'Unknown error' }));
        console.error('❌ Settings API error:', response.status, errorData);
        // Use defaults on error
        setTenantSettings({
          ...DEFAULT_TENANT_SETTINGS,
        });
      }
    } catch (error) {
      console.error('💥 Error loading tenant settings:', error);
      // Use defaults on error
      setTenantSettings({
        ...DEFAULT_TENANT_SETTINGS,
      });
    }
  };

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
  const handleAddPage = useCallback((index?: number) => {
    const newPage = createDefaultPage();

    setSurvey((prev) => {
      const newPages = [...prev.pages];
      const insertIndex = typeof index === 'number' ? index : newPages.length;

      // Insert the new page at the specified index
      newPages.splice(insertIndex, 0, newPage);

      // Recalculate titles and orders for all pages
      return {
        ...prev,
        pages: newPages.map((p, idx) => ({
          ...p,
          title: p.title.startsWith('Page ') ? `Page ${idx + 1}` : p.title,
          order: idx
        })),
      };
    });

    setHasUnsavedChanges(true);
    toast.success('Page added');
  }, []);

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

  // Bulk import questions from library
  const handleBulkImportQuestions = useCallback((newQuestions: any[], startIndex?: number) => {
    setSurvey((prev) => {
      const newPages = [...prev.pages];
      const insertAt = typeof startIndex === 'number' ? startIndex + 1 : newPages.length;

      const importedPages: SurveyPage[] = [];

      newQuestions.forEach((q, index) => {
        // Map backend type to frontend type
        const typeMap: any = {
          'Text': 'text',
          'Rating': 'rating',
          'MultipleChoice': 'single-choice',
          'Checkbox': 'multiple-choice',
          'Dropdown': 'dropdown'
        };
        const mappedType = typeMap[q.QuestionType] || 'text';

        // Use tenant default if exists
        const questionType = tenantSettings?.defaultQuestionType || mappedType;

        // Default config
        const defaults: any = {
          'rating': { ratingMin: 1, ratingMax: 5, ratingStep: 1, ratingLabels: { min: 'Never', max: 'Always' } },
          'single-choice': { options: [{ id: 'opt1', value: 1, text: 'Option 1', order: 0, score: 1 }, { id: 'opt2', value: 2, text: 'Option 2', order: 1, score: 2 }] },
          'multiple-choice': { options: [{ id: 'opt1', value: 1, text: 'Option 1', order: 0, score: 1 }, { id: 'opt2', value: 2, text: 'Option 2', order: 1, score: 2 }], minSelections: 0 },
          'text': { maxLength: 500, placeholder: 'Enter your answer...' },
          'textarea': { maxLength: 2000, placeholder: 'Enter your answer...' },
          'dropdown': { options: [{ id: 'opt1', value: 1, text: 'Option 1', order: 0, score: 1 }, { id: 'opt2', value: 2, text: 'Option 2', order: 1, score: 2 }] },
        };

        let config = defaults[questionType] || {};

        // Apply tenant rating options if applicable
        if (questionType === 'rating' && tenantSettings?.defaultRatingOptions?.length) {
          config = {
            ratingOptions: tenantSettings.defaultRatingOptions.map(opt => ({
              id: opt.id,
              value: opt.score ?? opt.order + 1,
              text: opt.text,
              order: opt.order,
              score: opt.score ?? opt.order + 1,
            })),
          };
        }

        const newSurveyQuestion: Question = {
          id: `q_${Date.now()}_${index}`,
          name: `question_${Date.now()}_${index}`,
          type: questionType,
          selfText: q.SelfQuestion,
          othersText: q.OthersQuestion,
          required: false,
          order: 0,
          config: config,
          showTo: 'everyone',
          importedFrom: {
            questionId: q.Id,
            clusterId: q.ClusterId || '',
            competencyId: q.CompetencyId,
          }
        };

        const newPage: SurveyPage = {
          id: generatePageId(),
          name: `page_${Date.now()}_${index}`,
          title: `Page ${newPages.length + 1}`,
          description: '',
          order: newPages.length,
          questions: [newSurveyQuestion]
        };

        importedPages.push(newPage);
      });

      // Insert all imported pages at the specified position
      newPages.splice(insertAt, 0, ...importedPages);

      return {
        ...prev,
        pages: newPages.map((p, idx) => ({
          ...p,
          title: p.title.startsWith('Page ') ? `Page ${idx + 1}` : p.title,
          order: idx
        }))
      };
    });

    setHasUnsavedChanges(true);
    toast.success(`Imported ${newQuestions.length} questions on ${newQuestions.length} new pages`);
  }, [tenantSettings]);

  // Helper to get all imported question IDs for "already imported" check
  const getExistingQuestionIds = useCallback(() => {
    const ids: string[] = [];
    survey.pages.forEach(p => {
      p.questions.forEach(q => {
        if (q.importedFrom?.questionId) {
          ids.push(q.importedFrom.questionId);
        }
      });
    });
    return ids;
  }, [survey.pages]);

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
              className={`px-6 py-3 font-medium border-b-2 transition-colors ${activeTab === 'form'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-600 hover:text-gray-900'
                }`}
            >
              Form Editor
            </button>
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
                setActiveTab('settings');
              }}
              className={`px-6 py-3 font-medium border-b-2 transition-colors ${activeTab === 'settings'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-600 hover:text-gray-900'
                }`}
            >
              Settings
            </button>
            <button
              onClick={handleSwitchToJsonTab}
              className={`px-6 py-3 font-medium border-b-2 transition-colors ${activeTab === 'json'
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
              {/* {!surveyId && (
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
              )} */}
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
                tenantSettings={tenantSettings}
                onUpdate={(updatedPage: SurveyPage) => handleUpdatePage(page.id, updatedPage)}
                onDelete={() => handleDeletePage(page.id)}
                onAddPageBelow={() => handleAddPage(index + 1)}
                onBulkImport={(questions) => handleBulkImportQuestions(questions, index)}
                existingQuestionIds={getExistingQuestionIds()}
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
              onClick={() => handleAddPage()}
              className="w-full py-3 text-black border-2 border-dashed border-gray-300 rounded-lg text-gray-600 hover:border-blue-500 hover:text-blue-600 transition-colors"
            >
              + Add Page
            </button>
          </>
        ) : activeTab === 'settings' ? (
          /* Settings Tab */
          <SurveySettingsTab tenantSlug={tenantSlug} token={token} />
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

