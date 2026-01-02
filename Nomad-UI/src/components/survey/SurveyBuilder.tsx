/* eslint-disable @typescript-eslint/no-unused-vars */
/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useEffect, useState, useRef } from 'react';
import { SurveyCreatorComponent, SurveyCreator } from 'survey-creator-react';
import { ICreatorPlugin } from 'survey-creator-core';
import { Serializer } from 'survey-core';
import 'survey-core/survey-core.min.css';
import 'survey-creator-core/survey-creator-core.min.css';
import toast from 'react-hot-toast';
import ImportQuestionModal from './ImportQuestionModal';

// Sky-Blue Custom Theme Configuration
const SKY_BLUE_THEME = {
  isPanelless: true,
  cssVariables: {
    // Primary Colors - Sky Blue Theme
    '--sjs-primary-backcolor': 'rgba(2, 132, 199, 1)', // #0284C7
    '--sjs-primary-backcolor-light': 'rgba(2, 132, 199, 0.1)',
    '--sjs-primary-backcolor-dark': 'rgba(3, 105, 161, 1)', // Darker sky blue
    '--sjs-primary-forecolor': 'rgba(255, 255, 255, 1)',
    '--sjs-primary-forecolor-light': 'rgba(255, 255, 255, 0.25)',

    // Secondary Colors - Light Sky Blue
    '--sjs-secondary-backcolor': 'rgba(224, 242, 254, 1)', // #E0F2FE
    '--sjs-secondary-backcolor-light': 'rgba(224, 242, 254, 0.5)',
    '--sjs-secondary-backcolor-semi-light': 'rgba(224, 242, 254, 0.75)',
    '--sjs-secondary-forecolor': 'rgba(15, 23, 42, 1)', // #0F172A - Black text
    '--sjs-secondary-forecolor-light': 'rgba(15, 23, 42, 0.6)',

    // General Background Colors - Gray instead of black
    '--sjs-general-backcolor': 'rgba(249, 250, 251, 1)', // #F9FAFB - Light gray
    '--sjs-general-backcolor-dark': 'rgba(229, 231, 235, 1)', // #E5E7EB - Gray-200
    '--sjs-general-backcolor-dim': 'rgba(243, 244, 246, 1)', // #F3F4F6 - Gray-100
    '--sjs-general-backcolor-dim-light': 'rgba(249, 250, 251, 1)', // #F9FAFB
    '--sjs-general-backcolor-dim-dark': 'rgba(229, 231, 235, 1)', // #E5E7EB

    // General Foreground Colors - Black text
    '--sjs-general-forecolor': 'rgba(0, 0, 0, 1)', // #000000 - Black text
    '--sjs-general-forecolor-light': 'rgba(75, 85, 99, 1)', // #4B5563 - Gray-600
    '--sjs-general-dim-forecolor': 'rgba(0, 0, 0, 1)', // #000000 - Black text
    '--sjs-general-dim-forecolor-light': 'rgba(75, 85, 99, 1)', // #4B5563

    // Font Colors - Black text everywhere
    '--sjs-font-editorfont-color': 'rgba(0, 0, 0, 1)', // #000000 - Black
    '--sjs-font-editorfont-placeholdercolor': 'rgba(107, 114, 128, 1)', // Gray-500
    '--sjs-font-questiontitle-color': 'rgba(0, 0, 0, 1)', // #000000 - Black
    '--sjs-font-questiondescription-color': 'rgba(55, 65, 81, 1)', // Gray-700
    '--sjs-font-questiontitle-weight': '600',
    '--sjs-font-questiondescription-size': '14px',

    // Editor and Question Backgrounds - White/Light Gray
    '--sjs-editor-background': 'rgba(255, 255, 255, 1)', // White
    '--sjs-editorpanel-backcolor': 'rgba(249, 250, 251, 1)', // #F9FAFB - Light gray
    '--sjs-editorpanel-hovercolor': 'rgba(243, 244, 246, 1)', // #F3F4F6 - Gray-100
    '--sjs-question-background': 'rgba(255, 255, 255, 1)', // White
    '--sjs-questionpanel-backcolor': 'rgba(249, 250, 251, 1)', // #F9FAFB - Light gray
    '--sjs-questionpanel-hovercolor': 'rgba(243, 244, 246, 1)', // #F3F4F6 - Gray-100

    // Border and Shadow
    '--sjs-border-light': 'rgba(226, 232, 240, 1)', // Slate-200
    '--sjs-border-default': 'rgba(203, 213, 225, 1)', // Slate-300
    '--sjs-border-inside': 'rgba(203, 213, 225, 1)',
    '--sjs-shadow-small': '0px 0px 0px 1px rgba(226, 232, 240, 1)',
    '--sjs-shadow-medium': '0px 2px 8px 0px rgba(0, 0, 0, 0.08)',
    '--sjs-shadow-large': '0px 8px 16px 0px rgba(0, 0, 0, 0.1)',
    '--sjs-shadow-inner': '0px 0px 0px 1px rgba(226, 232, 240, 1)',

    // Corner Radius
    '--sjs-corner-radius': '0.75rem', // 12px - Tailwind rounded-xl
    '--sjs-base-unit': '8px',

    // Special Colors
    '--sjs-special-red': 'rgba(239, 68, 68, 1)', // Red-500
    '--sjs-special-red-light': 'rgba(152, 142, 142, 0.1)',
    '--sjs-special-red-forecolor': 'rgba(255, 255, 255, 1)',
    '--sjs-special-green': 'rgba(34, 197, 94, 1)', // Green-500
    '--sjs-special-green-light': 'rgba(34, 197, 94, 0.1)',
    '--sjs-special-green-forecolor': 'rgba(255, 255, 255, 1)',
    '--sjs-special-blue': 'rgba(2, 132, 199, 1)', // Sky-600
    '--sjs-special-blue-light': 'rgba(2, 132, 199, 0.1)',
    '--sjs-special-blue-forecolor': 'rgba(255, 255, 255, 1)',
    '--sjs-special-yellow': 'rgba(234, 179, 8, 1)', // Yellow-500
    '--sjs-special-yellow-light': 'rgba(234, 179, 8, 0.1)',
    '--sjs-special-yellow-forecolor': 'rgba(255, 255, 255, 1)',

    // Article Font Sizes (Optional - for modern look)
    '--sjs-article-font-xx-large-fontWeight': '700',
    '--sjs-article-font-xx-large-lineHeight': '64px',
    '--sjs-article-font-x-large-fontWeight': '700',
    '--sjs-article-font-x-large-lineHeight': '56px',
    '--sjs-article-font-large-fontWeight': '600',
    '--sjs-article-font-large-lineHeight': '40px',
    '--sjs-article-font-medium-fontWeight': '600',
    '--sjs-article-font-medium-lineHeight': '32px',
    '--sjs-article-font-default-fontWeight': '400',
    '--sjs-article-font-default-lineHeight': '28px',
  },
  themeName: 'sky-blue-custom',
  colorPalette: 'light',
};

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
  const [showImportModal, setShowImportModal] = useState(false);
  const [autoAssign, setAutoAssign] = useState<boolean>(false);

  // Optional: Export current theme configuration
  const exportTheme = () => {
    if (!creator) return;

    const currentTheme = creator.survey.getTheme();
    console.log('Current Survey Theme:', JSON.stringify(currentTheme, null, 2));

    // Optional: Save to localStorage for persistence
    localStorage.setItem('surveyjs-custom-theme', JSON.stringify(currentTheme));

    toast.success('Theme exported to console and localStorage');
  };

  useEffect(() => {
    const options = {
      showLogicTab: true,
      showTranslationTab: false,
      isAutoSave: false,
    };

    // Register score property for options (itemvalues)
    Serializer.addProperty("itemvalue", {
      name: "score:number",
      displayName: "Score",
      default: 0,
      visibleIndex: 0 // Show at top of property grid for items
    });

    const surveyCreator = new SurveyCreator(options);

    // Apply Sky-Blue Custom Theme to the survey
    surveyCreator.survey.applyTheme(SKY_BLUE_THEME);

    // Set initial survey if provided
    if (initialSurvey) {
      surveyCreator.JSON = initialSurvey.Schema || {};
      // Reapply theme after loading JSON
      surveyCreator.survey.applyTheme(SKY_BLUE_THEME);
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

    // Add custom "Import Question" action to question toolbar
    surveyCreator.onDefineElementMenuItems.add((_sender: any, options: any) => {
      // Only add to questions, not pages or panels
      const objType = options.obj.getType();
      if (objType !== 'page' && objType !== 'panel' && options.obj.name) {
        // Find the index of "Delete" action to insert before it
        const deleteIndex = options.items.findIndex((item: any) => item.id === 'delete');
        const insertIndex = deleteIndex !== -1 ? deleteIndex : options.items.length;

        // Add Import Question action
        options.items.splice(insertIndex, 0, {
          id: 'import-question',
          title: 'Import Question',
          iconName: 'icon-import',
          action: () => {
            setShowImportModal(true);
          },
        });
      }
    });

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

  const handleImportQuestion = (question: any) => {
    if (!creator) return;

    try {
      const surveyJSON = creator.JSON;

      // Get the current page or create one if it doesn't exist
      if (!surveyJSON.pages || surveyJSON.pages.length === 0) {
        surveyJSON.pages = [{ name: 'page1', elements: [] }];
      }

      const currentPage = surveyJSON.pages[0];
      if (!currentPage.elements) {
        currentPage.elements = [];
      }

      // Generate unique question names
      const questionBaseName = `q_${question.Id.substring(0, 8)}`;
      const selfQuestionName = `${questionBaseName}_self`;
      const othersQuestionName = `${questionBaseName}_others`;

      // Map question types to SurveyJS types
      const questionTypeMap: Record<string, string> = {
        'Text': 'text',
        'Rating': 'rating',
        'MultipleChoice': 'radiogroup',
        'Checkbox': 'checkbox',
        'Dropdown': 'dropdown',
      };

      const surveyJSType = questionTypeMap[question.QuestionType] || 'text';

      // Add Self question with visibility condition
      const selfQuestion = {
        type: surveyJSType,
        name: selfQuestionName,
        title: question.SelfQuestion,
        visibleIf: "{relationship} = 'Self'",
        isRequired: false,
        questionId: question.Id, // Store reference to original question
        questionSource: 'imported',
      };

      // Add Others question with visibility condition
      const othersQuestion = {
        type: surveyJSType,
        name: othersQuestionName,
        title: question.OthersQuestion,
        visibleIf: "{relationship} <> 'Self'",
        isRequired: false,
        questionId: question.Id, // Store reference to original question
        questionSource: 'imported',
      };

      // Add both questions to the survey
      currentPage.elements.push(selfQuestion);
      currentPage.elements.push(othersQuestion);

      // Update the creator
      creator.JSON = surveyJSON;

      toast.success('Question imported successfully!');
    } catch (error: any) {
      console.error('Error importing question:', error);
      toast.error('Failed to import question');
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
    <div className="w-full h-full bg-gray-50 font-['Inter',_'Poppins',_system-ui,_-apple-system,_sans-serif]">
      <div className="survey-builder-container">
        {/* Action Buttons */}
        <div className="bg-white border-b border-gray-200 px-6 py-4">
          <div className="flex justify-between items-center">
            <div className="flex-1">
              <h2 className="text-xl font-semibold text-gray-900">
                {surveyId ? 'Edit Survey' : 'Create New Survey'}
              </h2>

              {/* Instructions */}
              <p className="text-sm text-gray-600 mt-2">
                Use drag & drop to build your survey. You can include placeholders like{' '}
                <code className="bg-gray-100 px-1 rounded">{'{subjectName}'}</code> or{' '}
                <code className="bg-gray-100 px-1 rounded">{'{evaluatorName}'}</code>{' '}
                in question text. Questions will be shown based on the evaluator&apos;s relationship to the subject.
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

          {/* Auto-Assign Option - Only show when creating new survey */}
          {!surveyId && (
            <div className="mt-4 p-4 bg-gray-50 border border-gray-200 rounded-lg">
              <label className="block text-sm font-medium text-gray-900 mb-3">
                Evaluator Assignment <span className="text-red-500">*</span>
              </label>
              <div className="space-y-2">
                <label className="flex items-center cursor-pointer">
                  <input
                    type="radio"
                    name="assignmentType"
                    value="manual"
                    checked={!autoAssign}
                    onChange={() => setAutoAssign(false)}
                    className="h-4 w-4 text-blue-600 border-gray-300 focus:ring-blue-500 cursor-pointer"
                  />
                  <span className="ml-3 text-sm text-gray-700">
                    Assign Evaluators Manually
                  </span>
                </label>
                <label className="flex items-center cursor-pointer">
                  <input
                    type="radio"
                    name="assignmentType"
                    value="auto"
                    checked={autoAssign}
                    onChange={() => setAutoAssign(true)}
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

        /* ===== SurveyJS Creator Theme Overrides - Gray Backgrounds with Black Text ===== */

        /* CRITICAL: Override all black backgrounds globally */
        .svc-creator,
        .svc-creator *,
        .svc-tab-designer,
        .svc-tab-designer *,
         
        .sd-root-modern,
        .sd-root-modern * {
          background-color: transparent !important;
        }

        /* Top Menu Tabs - Gray background with black text */
        .svc-tabbed-menu-item,
        .svc-tab-designer,
        .svc-tab-logic,
        .svc-tab-preview,
        .svc-tab-translation {
          background-color: #f3f4f6 !important; /* Gray-100 */
          color: #000000 !important; /* Black text */
        }

        .svc-tabbed-menu-item:hover {
          background-color: #e5e7eb !important; /* Gray-200 */
        }

        .svc-tabbed-menu-item--selected,
        .svc-tabbed-menu-item.svc-tabbed-menu-item--selected {
          background-color: #0284c7 !important; /* Sky-600 - Active tab */
          color: #ffffff !important; /* White text for active */
        }

        /* Add Question Button - Gray background with black text */
        .svc-question__content-actions,
        .svc-page__add-new-question,
        .svc-page__question-add-new-question,
        .svc-question__add-new-question,
        .svc-add-new-question-btn,
        .svc-add-new-item-button,
        .svc-item-value-controls__add,
        .svc-page__add-new-question-container,
        .svc-page__footer,
        .svc-question__drag-area,
        .svc-question__drag-element {
          background-color: #f3f4f6 !important; /* Gray-100 */
          color: #000000 !important; /* Black text */
          border: 1px solid #d1d5db !important; /* Gray-300 border */
        }

        .svc-question__content-actions:hover,
        .svc-page__add-new-question:hover,
        .svc-add-new-question-btn:hover,
        .svc-add-new-item-button:hover {
          background-color: #e5e7eb !important; /* Gray-200 on hover */
          color: #000000 !important;
        }

        /* Page header and title areas - White background */
        .svc-page__header,
        .svc-page__title,
        .sd-page__title,
        .sd-page__description,
        .svc-page__content-actions {
          background-color: #ffffff !important;
          color: #000000 !important;
        }

        /* Black bar at top of pages - make it white */
        .svc-page__content::before,
        .svc-page__content::after,
        .svc-question__content::before,
        .svc-question__content::after {
          background-color: #ffffff !important;
        }

        /* Property Grid (Right Panel) - Gray background with black text */
        .svc-property-panel,
        .svc-property-panel__header,
        .spg-panel,
        .spg-panel__content,
        .spg-question,
        .spg-row {
          background-color: #f9fafb !important; /* Light gray */
          color: #000000 !important; /* Black text */
        }

        .spg-panel__title,
        .spg-question__title,
        .spg-question__content {
          color: #000000 !important; /* Black text */
        }

        /* Toolbox (Left Panel) - Gray background */
        .svc-toolbox,
        .svc-toolbox__container,
        .svc-toolbox__category {
          background-color: #f9fafb !important; /* Light gray */
        }

        .svc-toolbox__category-header,
        .svc-toolbox__category-title {
          background-color: #e5e7eb !important; /* Gray-200 */
          color: #000000 !important; /* Black text */
        }

        /* Designer Surface - White/Light Gray */
        .svc-tab-designer__content,
        .svc-creator__content-wrapper,
        .svc-creator-tab__content,
        .svc-tab-designer__content-wrapper,
        .sd-body,
        .sd-container-modern,
        .sd-page,
        .sd-page__content {
          background-color: #ffffff !important; /* White */
        }

        /* Page elements - White background */
        .svc-page,
        .svc-page__content,
        .svc-page__content-wrapper,
        .sd-page__row,
        .sd-row {
          background-color: #ffffff !important;
        }

        /* Question containers - White background */
        .svc-question,
        .svc-question__content,
        .sd-question,
        .sd-element,
        .sd-element__content {
          background-color: #ffffff !important;
        }

        /* Remove all black backgrounds from survey elements */
        .sd-body,
        .sd-body__page,
        .sd-page,
        .sd-row,
        .sd-question,
        .sd-element,
        .sd-panel,
        .sd-panel__content,
        .svc-page__content,
        .svc-question__content,
        div[class*="svc-"],
        div[class*="sd-"] {
          background-color: transparent !important;
        }

        /* Ensure white background for main containers */
        .svc-tab-designer__content,
        .svc-creator__content-wrapper,
        .sd-root-modern__container,
        .sd-container-modern__content {
          background-color: #ffffff !important;
        }

        /* Page Navigator - Gray background */
        .svc-page-navigator,
        .svc-page-navigator__selector {
          background-color: #f3f4f6 !important; /* Gray-100 */
          color: #000000 !important;
        }

        .svc-page-navigator-item {
          background-color: #f9fafb !important;
          color: #000000 !important;
        }

        .svc-page-navigator-item--selected {
          background-color: #0284c7 !important; /* Sky-600 */
          color: #ffffff !important;
        }

        /* Question Adorners - White background */
        .svc-question__adorner,
        .svc-question__content,
        .svc-question__content-wrapper {
          background-color: #ffffff !important; /* White */
          border: 1px solid #e5e7eb !important; /* Light gray border */
        }

        /* Nuclear option: Override ANY element with black background */
        .svc-creator [style*="background-color: rgb(0, 0, 0)"],
        .svc-creator [style*="background-color: black"],
        .svc-creator [style*="background: rgb(0, 0, 0)"],
        .svc-creator [style*="background: black"],
        .sd-root-modern [style*="background-color: rgb(0, 0, 0)"],
        .sd-root-modern [style*="background-color: black"],
        .sd-root-modern [style*="background: rgb(0, 0, 0)"],
        .sd-root-modern [style*="background: black"] {
          background-color: #ffffff !important;
          background: #ffffff !important;
        }

        /* Specific fix for page description areas */
        .svc-page__description-container,
        .sd-page__description-container {
          background-color: #ffffff !important;
        }

        /* Fix for the black bars in the screenshot */
        .svc-page__content-actions-container,
        .svc-question__drag-area-indicator,
        .svc-question__drag-area-placeholder {
          background-color: #f3f4f6 !important; /* Light gray */
        }

        /* Action Buttons - Gray with black text */
        .svc-action-button,
        .sv-action-bar-item,
        .sv-action__content {
          background-color: #f3f4f6 !important; /* Gray-100 */
          color: #000000 !important;
        }

        .svc-action-button:hover,
        .sv-action-bar-item:hover {
          background-color: #e5e7eb !important; /* Gray-200 */
        }

        /* Dropdown menus - White background with black text */
        .sv-popup,
        .sv-popup__container,
        .sv-list,
        .sv-list__item {
          background-color: #ffffff !important;
          color: #000000 !important;
        }

        .sv-list__item:hover {
          background-color: #f3f4f6 !important; /* Gray-100 */
        }

        /* ===== FORCE ALL TEXT TO BLACK ===== */

        /* General text color override - BLACK everywhere */
        .svc-creator,
        .svc-creator *,
        .svc-tab-designer,
        .svc-tab-designer *,
        .svc-property-panel,
        .svc-property-panel *,
        .sd-root-modern,
        .sd-root-modern *,
        .svc-page,
        .svc-page *,
        .svc-question,
        .svc-question * {
          color: #000000 !important;
        }

        /* Ensure all text inputs in property grid are black */
        .spg-input,
        .spg-checkbox__caption,
        .spg-dropdown,
        .spg-text-editor {
          color: #000000 !important;
        }

        /* All labels and text elements - BLACK */
        label,
        span,
        p,
        div,
        h1, h2, h3, h4, h5, h6,
        .svc-text,
        .sd-text,
        .svc-string-viewer,
        .sd-string-viewer {
          color: #000000 !important;
        }

        /* Page and question titles - BLACK */
        .svc-page__title,
        .sd-page__title,
        .svc-question__title,
        .sd-question__title,
        .sd-element__title {
          color: #000000 !important;
          font-weight: 600 !important;
        }

        /* Descriptions - BLACK (not gray) */
        .svc-page__description,
        .sd-page__description,
        .svc-question__description,
        .sd-question__description,
        .sd-element__description {
          color: #000000 !important;
        }

        /* Empty state text - BLACK */
        .svc-page__placeholder,
        .svc-question__placeholder,
        .svc-empty-message,
        .sd-empty-message {
          color: #000000 !important;
        }

        /* Property grid labels - BLACK */
        .spg-label,
        .spg-title,
        .spg-question__title,
        .spg-panel__title {
          color: #000000 !important;
        }

        /* Toolbox item text - BLACK */
        .svc-toolbox__item-title,
        .svc-toolbox__item-text,
        .svc-toolbox__category-title {
          color: #000000 !important;
        }

        /* SurveyJS Form Theme - Blue and White with Black Text */

        /* Question text - Black */
        .sd-question__title,
        .sd-question__header,
        .sd-element__title,
        .sd-title,
        .sd-page__title,
        .sd-survey__title,
        .svc-page__title,
        .svc-question__title {
          color: #000000 !important;
          font-weight: 600 !important;
        }

        /* Question description - BLACK (not gray) */
        .sd-question__description,
        .sd-element__description,
        .svc-page__description,
        .svc-question__description {
          color: #000000 !important;
        }

        /* "Description" label text - BLACK */
        .svc-page__description-label,
        .svc-question__description-label,
        .sd-description,
        .svc-string-viewer__text {
          color: #000000 !important;
        }

        /* Empty state message - BLACK */
        .svc-page__placeholder-text,
        .svc-question__placeholder-text,
        .svc-empty-state,
        .svc-empty-state__text {
          color: #000000 !important;
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
          color: #b83535ff !important;
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

        /* ===== FINAL NUCLEAR OVERRIDES - Remove ALL black backgrounds ===== */

        /* Override any remaining black backgrounds in SurveyJS */
        .svc-creator div,
        .svc-creator section,
        .svc-creator article,
        .sd-root-modern div,
        .sd-root-modern section {
          background-color: inherit !important;
        }

        /* Force white background on main content areas */
        .svc-tab-designer__content,
        .svc-creator__content-holder,
        .svc-creator__area,
        .sd-root-modern {
          background-color: #ffffff !important;
        }

        /* Force white/gray on all survey elements */
        .svc-page,
        .svc-page > *,
        .svc-question,
        .svc-question > * {
          background-color: #ffffff !important;
        }

        /* Remove black from any pseudo-elements */
        .svc-creator *::before,
        .svc-creator *::after,
        .sd-root-modern *::before,
        .sd-root-modern *::after {
          background-color: transparent !important;
        }

        /* Specific override for the black bars visible in screenshot */
        .svc-page__content > div,
        .svc-question__content > div {
          background-color: #ffffff !important;
        }

        /* Override SurveyJS default dark theme if applied */
        .sv-root-modern--dark,
        .svc-creator--dark {
          background-color: #ffffff !important;
        }

        .sv-root-modern--dark *,
        .svc-creator--dark * {
          background-color: inherit !important;
          color: #000000 !important;
        }

        /* ===== FINAL TEXT COLOR OVERRIDES - ALL TEXT BLACK ===== */

        /* Override any gray/light text colors */
        .svc-creator [class*="text"],
        .svc-creator [class*="title"],
        .svc-creator [class*="label"],
        .svc-creator [class*="description"],
        .svc-creator [class*="placeholder"],
        .sd-root-modern [class*="text"],
        .sd-root-modern [class*="title"],
        .sd-root-modern [class*="label"],
        .sd-root-modern [class*="description"] {
          color: #000000 !important;
        }

        /* Specific override for "Description" text */
        .svc-page__description-container *,
        .svc-question__description-container *,
        .sd-page__description *,
        .sd-question__description * {
          color: #000000 !important;
        }

        /* Override for empty state text like "The page is empty..." */
        .svc-page__content-actions-text,
        .svc-question__content-actions-text,
        .svc-panel__placeholder,
        .svc-panel__add-new-question-text {
          color: #000000 !important;
        }

        /* All text nodes in SurveyJS - NUCLEAR OPTION */
        .svc-creator,
        .svc-creator > *,
        .svc-creator > * > *,
        .svc-creator > * > * > *,
        .sd-root-modern,
        .sd-root-modern > *,
        .sd-root-modern > * > *,
        .sd-root-modern > * > * > * {
          color: #000000 !important;
        }

        /* Override inline text color styles */
        .svc-creator [style*="color"],
        .sd-root-modern [style*="color"] {
          color: #000000 !important;
        }

        /* Ensure white text only on blue/active elements */
        .svc-tabbed-menu-item--selected,
        .svc-tabbed-menu-item--selected *,
        .svc-page-navigator-item--selected,
        .svc-page-navigator-item--selected *,
        .sd-btn,
        .sd-btn *,
        button.sd-navigation__complete-btn,
        button.sd-navigation__next-btn {
          color: #ffffff !important;
        }
      `}</style>

        {/* Import Question Modal */}
        <ImportQuestionModal
          isOpen={showImportModal}
          onClose={() => setShowImportModal(false)}
          onImport={handleImportQuestion}
          tenantSlug={tenantSlug}
          token={token}
        />
      </div>
    </div>
  );
}

