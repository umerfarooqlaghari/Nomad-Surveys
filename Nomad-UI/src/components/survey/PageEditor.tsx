/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { SurveyPage, Question, createDefaultQuestion, generateQuestionId, TenantSettings, DEFAULT_QUESTION_CONFIGS } from '@/types/survey';
import QuestionEditor from './QuestionEditor';
import ImportQuestionModal from './ImportQuestionModal';
import toast from 'react-hot-toast';

interface PageEditorProps {
  page: SurveyPage;
  pageNumber: number;
  totalPages: number;
  tenantSlug: string;
  token: string;
  tenantSettings: TenantSettings | null;
  onUpdate: (updatedPage: SurveyPage) => void;
  onDelete: () => void;
  onAddPageBelow?: () => void;
  onBulkImport?: (questions: any[], index?: number) => void;
  existingQuestionIds?: string[];
  onMoveUp?: () => void;
  onMoveDown?: () => void;
}


export default function PageEditor({
  page,
  pageNumber,
  totalPages,
  tenantSlug,
  token,
  tenantSettings,
  onUpdate,
  onDelete,
  onAddPageBelow,
  onBulkImport,
  existingQuestionIds,
  onMoveUp,
  onMoveDown,
}: PageEditorProps) {
  const [isExpanded, setIsExpanded] = useState(true);
  const [isEditingTitle, setIsEditingTitle] = useState(false);
  const [showImportModal, setShowImportModal] = useState(false);

  // Log when tenantSettings changes
  useEffect(() => {
    console.log('🔄 [PageEditor] tenantSettings updated:', tenantSettings);
  }, [tenantSettings]);

  // Update page title
  const handleTitleChange = (title: string) => {
    onUpdate({ ...page, title });
  };

  // Update page description
  const handleDescriptionChange = (description: string) => {
    onUpdate({ ...page, description });
  };

  // Add a new custom question
  const handleAddQuestion = (type: Question['type']) => {
    // Use tenant settings if available, otherwise use the provided type
    const questionType = tenantSettings?.defaultQuestionType || type;
    const newQuestion = createDefaultQuestion(questionType);

    console.log('🔧 Adding question:', {
      requestedType: type,
      actualType: questionType,
      tenantSettings,
      hasRatingOptions: tenantSettings?.defaultRatingOptions?.length,
    });

    // If the question type is rating and we have custom rating options, replace the config
    if (questionType === 'rating' && tenantSettings?.defaultRatingOptions && tenantSettings.defaultRatingOptions.length > 0) {
      newQuestion.config = {
        ratingOptions: tenantSettings.defaultRatingOptions.map(opt => ({
          id: opt.id,
          value: opt.score ?? opt.order + 1, // Explicitly use score as value
          text: opt.text,
          order: opt.order,
          score: opt.score ?? opt.order + 1,
        })),
      };
      console.log('✅ Applied rating options:', newQuestion.config.ratingOptions);
    }

    newQuestion.order = page.questions.length;

    onUpdate({
      ...page,
      questions: [...page.questions, newQuestion],
    });
    toast.success('Question added');
  };

  // Import question from library
  const handleImportQuestion = (importedQuestions: any[]) => {
    if (onBulkImport) {
      onBulkImport(importedQuestions, pageNumber - 1);
    } else {
      // Fallback for single import if builder doesn't support bulk yet
      // though ideally CustomSurveyBuilder handles this now.
      toast.error('Bulk import not supported by builder');
    }
  };

  // Update a question
  const handleUpdateQuestion = (questionId: string, updatedQuestion: Question) => {
    onUpdate({
      ...page,
      questions: page.questions.map((q) => (q.id === questionId ? updatedQuestion : q)),
    });
  };

  // Delete a question
  const handleDeleteQuestion = (questionId: string) => {
    if (!confirm('Are you sure you want to delete this question?')) {
      return;
    }

    onUpdate({
      ...page,
      questions: page.questions
        .filter((q) => q.id !== questionId)
        .map((q, index) => ({ ...q, order: index })),
    });
    toast.success('Question deleted');
  };

  // Reorder questions
  const handleReorderQuestions = (reorderedQuestions: Question[]) => {
    onUpdate({
      ...page,
      questions: reorderedQuestions.map((q, index) => ({ ...q, order: index })),
    });
  };

  // Helper function to map backend question types to our types
  const mapQuestionType = (backendType: string): Question['type'] => {
    const typeMap: Record<string, Question['type']> = {
      'Text': 'text',
      'Rating': 'rating',
    };
    return typeMap[backendType] || 'text';
  };

  // Helper function to get default config for a question type
  const getDefaultConfigForType = (type: Question['type']) => {
    const defaults: Record<Question['type'], any> = {
      'rating': { ratingMin: 1, ratingMax: 5, ratingStep: 1, ratingLabels: { min: 'Never', max: 'Always' } },
      'text': { maxLength: 500, placeholder: 'Enter your answer...' },
      'textarea': { maxLength: 2000, placeholder: 'Enter your answer...' },
    };
    return defaults[type] || {};
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 mb-6">
      {/* Page Header */}
      <div className="border-b border-gray-200 p-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3 flex-1">
            {/* Expand/Collapse Button */}
            <button
              onClick={() => setIsExpanded(!isExpanded)}
              className="text-gray-500 hover:text-gray-700"
            >
              <svg
                className={`w-5 h-5 transition-transform ${isExpanded ? 'rotate-90' : ''}`}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </button>

            {/* Page Title */}
            {isEditingTitle ? (
              <input
                type="text"
                value={page.title}
                onChange={(e) => handleTitleChange(e.target.value)}
                onBlur={() => setIsEditingTitle(false)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') setIsEditingTitle(false);
                }}
                autoFocus
                className="flex-1 px-2 py-1 border border-blue-500 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            ) : (
              <h3
                onClick={() => setIsEditingTitle(true)}
                className="text-lg font-semibold text-gray-900 cursor-pointer hover:text-blue-600"
              >
                {page.title} - {page.questions.length} Question{page.questions.length !== 1 ? 's' : ''}
              </h3>
            )}
          </div>

          {/* Page Actions */}
          <div className="flex items-center gap-2">
            {onMoveUp && (
              <button
                onClick={onMoveUp}
                className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded"
                title="Move page up"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
                </svg>
              </button>
            )}
            {onMoveDown && (
              <button
                onClick={onMoveDown}
                className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded"
                title="Move page down"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </button>
            )}
            {totalPages > 1 && (
              <button
                onClick={onDelete}
                className="p-2 text-red-500 hover:text-red-700 hover:bg-red-50 rounded"
                title="Delete page"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            )}
          </div>
        </div>

        {/* Page Description */}
        {isExpanded && (
          <div className="mt-3">
            <textarea
              value={page.description || ''}
              onChange={(e) => handleDescriptionChange(e.target.value)}
              placeholder="Page description (optional)..."
              rows={2}
              className="w-full px-3 py-2 text-black border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        )}
      </div>

      {/* Questions */}
      {isExpanded && (
        <div className="p-4">
          {page.questions.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <p className="mb-4">No questions yet. Add your first question!</p>
            </div>
          ) : (
            <div className="space-y-4">
              {page.questions.map((question, index) => (
                <QuestionEditor
                  key={question.id}
                  question={question}
                  questionNumber={index + 1}
                  totalQuestions={page.questions.length}
                  onUpdate={(updatedQuestion) => handleUpdateQuestion(question.id, updatedQuestion)}
                  onDelete={() => handleDeleteQuestion(question.id)}
                  onMoveUp={
                    index > 0
                      ? () => {
                        const newQuestions = [...page.questions];
                        [newQuestions[index - 1], newQuestions[index]] = [newQuestions[index], newQuestions[index - 1]];
                        handleReorderQuestions(newQuestions);
                      }
                      : undefined
                  }
                  onMoveDown={
                    index < page.questions.length - 1
                      ? () => {
                        const newQuestions = [...page.questions];
                        [newQuestions[index], newQuestions[index + 1]] = [newQuestions[index + 1], newQuestions[index]];
                        handleReorderQuestions(newQuestions);
                      }
                      : undefined
                  }
                />
              ))}
            </div>
          )}

          {/* Add Question Buttons */}
          <div className="mt-6 flex gap-3">
            <button
              onClick={() => setShowImportModal(true)}
              className="flex-1 py-3 border-2 border-blue-500 text-blue-600 rounded-lg hover:bg-blue-50 transition-colors font-medium"
            >
              Import from Library
            </button>
            <button
              onClick={() => handleAddQuestion('rating')}
              className="flex-1 py-3 border-2 border-dashed border-gray-300 text-gray-600 rounded-lg hover:border-gray-400 hover:text-gray-700 transition-colors"
            >
              + Add New Question
            </button>
          </div>
        </div>
      )}

      {/* Import Question Modal */}
      <ImportQuestionModal
        isOpen={showImportModal}
        onClose={() => setShowImportModal(false)}
        onImport={handleImportQuestion}
        tenantSlug={tenantSlug}
        token={token}
        existingQuestionIds={existingQuestionIds}
      />

      {/* Add Page Below Button */}
      {onAddPageBelow && (
        <div className="mt-4 px-4 pb-4">
          <button
            onClick={onAddPageBelow}
            className="w-full py-2 flex items-center justify-center gap-2 text-sm font-medium text-blue-600 bg-blue-50 border border-blue-200 rounded-lg hover:bg-blue-100 transition-colors"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            Add Page Below
          </button>
        </div>
      )}
    </div>
  );
}

