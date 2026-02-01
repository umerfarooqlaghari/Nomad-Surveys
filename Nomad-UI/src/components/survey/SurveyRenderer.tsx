/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState } from 'react';
import { SurveySchema, Question } from '@/types/survey';
import QuestionRenderer from './QuestionRenderer';

interface SurveyRendererProps {
  survey: SurveySchema;
  relationshipType: string; // 'Self', 'Manager', 'Peer', etc.
  isPreview?: boolean;
  initialData?: Record<string, any>;
  onDataChange?: (data: Record<string, any>) => void;
  onSubmit?: () => void | Promise<void>;
  showHeader?: boolean; // Whether to show the survey title/description header
}

export default function SurveyRenderer({
  survey,
  relationshipType,
  isPreview = false,
  initialData = {},
  onDataChange,
  onSubmit,
  showHeader = true,
}: SurveyRendererProps) {
  const [currentPageIndex, setCurrentPageIndex] = useState(0);
  const [responses, setResponses] = useState<Record<string, any>>(initialData);

  const isSelf = relationshipType === 'Self';

  // Filter questions based on showTo and relationship
  const getVisibleQuestionsForPage = (questions: Question[]) => {
    return questions.filter((q) => {
      // Hide from self-evaluator if selfText is empty (optional self question)
      if (isSelf && (!q.selfText || !q.selfText.trim())) {
        return false;
      }

      if (!q.showTo || q.showTo === 'everyone') return true;
      if (q.showTo === 'self' && isSelf) return true;
      if (q.showTo === 'others' && !isSelf) return true;
      return false;
    });
  };

  // Calculate active pages (pages with at least one visible question)
  const activePages = React.useMemo(() => {
    return survey.pages.filter(page => {
      const visibleQs = getVisibleQuestionsForPage(page.questions);
      return visibleQs.length > 0;
    });
  }, [survey.pages, isSelf]);

  // Reset page index when active pages change to prevent out-of-bounds errors
  React.useEffect(() => {
    setCurrentPageIndex(0);
  }, [activePages.length, relationshipType]);

  // Calculate progress metrics
  const progressMetrics = React.useMemo(() => {
    // 1. Get all visible questions across all active pages
    const allVisibleQuestions = activePages.flatMap(page => getVisibleQuestionsForPage(page.questions));
    const total = allVisibleQuestions.length;

    // 2. Count answered questions
    const answered = allVisibleQuestions.filter(q => {
      const answer = responses[q.id];
      if (answer === undefined || answer === null || answer === '') return false;
      if (Array.isArray(answer) && answer.length === 0) return false;
      return true;
    }).length;

    const percentage = total > 0 ? Math.round((answered / total) * 100) : 0;

    return { total, answered, percentage };
  }, [activePages, responses]);

  const currentPage = activePages[currentPageIndex];

  // Helper to get questions for the current page
  const visibleQuestions = currentPage ? getVisibleQuestionsForPage(currentPage.questions) : [];

  // Handle answer change
  const handleAnswerChange = (questionId: string, value: any) => {
    const newResponses = { ...responses, [questionId]: value };
    setResponses(newResponses);

    if (onDataChange) {
      onDataChange(newResponses);
    }
  };

  // Check if current page is complete (all required questions answered)
  const isPageComplete = () => {
    if (isPreview) return true;
    if (!currentPage) return true;

    return visibleQuestions.every((q) => {
      if (!q.required) return true;
      const answer = responses[q.id];
      if (answer === undefined || answer === null || answer === '') return false;
      if (Array.isArray(answer) && answer.length === 0) return false;
      return true;
    });
  };

  // Check if the entire survey is complete (all required questions across all active pages)
  const isEverythingComplete = () => {
    if (isPreview) return true;

    return activePages.every(page => {
      const questions = getVisibleQuestionsForPage(page.questions);
      return questions.every(q => {
        if (!q.required) return true;
        const answer = responses[q.id];
        if (answer === undefined || answer === null || answer === '') return false;
        if (Array.isArray(answer) && answer.length === 0) return false;
        return true;
      });
    });
  };

  // Get list of incomplete page indices
  const getIncompletePages = () => {
    return activePages
      .map((page, idx) => {
        const questions = getVisibleQuestionsForPage(page.questions);
        const isComplete = questions.every(q => {
          if (!q.required) return true;
          const answer = responses[q.id];
          if (answer === undefined || answer === null || answer === '') return false;
          if (Array.isArray(answer) && answer.length === 0) return false;
          return true;
        });
        return isComplete ? null : idx + 1;
      })
      .filter((idx): idx is number => idx !== null);
  };

  // Navigation handlers
  const handleNext = () => {
    if (currentPageIndex < activePages.length - 1) {
      setCurrentPageIndex(currentPageIndex + 1);
    }
  };

  const handlePrevious = () => {
    if (currentPageIndex > 0) {
      setCurrentPageIndex(currentPageIndex - 1);
    }
  };

  if (activePages.length === 0) {
    return (
      <div className="max-w-3xl mx-auto p-6 bg-white rounded-lg border border-gray-200">
        <p className="text-gray-600 text-center">No questions available for this survey.</p>
      </div>
    )
  }

  // Use the original page index from the full survey for display purposes if needed, 
  // currently we just use 1-based index of active pages which is usually better for UX.
  const displayPageIndex = currentPageIndex + 1;
  const totalDisplayPages = activePages.length;

  // Helper to calculate the starting number for the current page's questions
  const getQuestionStartIndex = (pageIndex: number) => {
    let count = 0;
    for (let i = 0; i < pageIndex; i++) {
      count += getVisibleQuestionsForPage(activePages[i].questions).length;
    }
    return count;
  };

  return (
    <div className={showHeader ? "max-w-3xl mx-auto" : ""}>
      {/* Survey Header */}
      {showHeader && (
        <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
          <h1 className="text-2xl font-bold text-gray-900 mb-2">{survey.title}</h1>
          {survey.description && (
            <p className="text-gray-600">{survey.description}</p>
          )}
          {isPreview && (
            <div className="mt-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
              <p className="text-sm text-blue-800">
                <strong>Preview Mode:</strong> Viewing as {isSelf ? 'Self' : relationshipType}
              </p>
            </div>
          )}
        </div>
      )}

      {/* Progress Bar Area */}
      <div className={`${showHeader ? "bg-white rounded-lg border border-gray-200 p-6 mb-6" : "bg-white rounded-lg border border-gray-200 p-4 mb-6 shadow-sm sticky top-0 z-10"}`}>
        <div className="flex justify-between items-center mb-2">
          <span className="text-sm font-medium text-gray-700">Survey Progress</span>
          <span className="text-sm font-semibold text-blue-600">{progressMetrics.percentage}% Complete</span>
        </div>
        <div className="w-full bg-gray-200 rounded-full h-2.5">
          <div
            className="bg-blue-600 h-2.5 rounded-full transition-all duration-500 ease-out"
            style={{ width: `${progressMetrics.percentage}%` }}
          ></div>
        </div>
        <div className="mt-2 text-[11px] text-gray-500 flex justify-between">
          <span>{progressMetrics.answered} of {progressMetrics.total} questions answered</span>
          {progressMetrics.percentage === 100 && (
            <span className="text-green-600 font-medium flex items-center">
              Ready to submit!
            </span>
          )}
        </div>
      </div>

      {/* Page Content */}
      <div className={showHeader ? "bg-white rounded-lg border border-gray-200 p-6 mb-6" : "mb-6"}>
        {/* Page Header */}
        <div className="mb-6">
          <h2 className="text-xl font-semibold text-gray-900">{currentPage.title}</h2>
          {currentPage.description && (
            <p className="text-gray-600 mt-2">{currentPage.description}</p>
          )}
          <div className="mt-3 text-sm text-gray-500">
            Page {displayPageIndex} of {totalDisplayPages}
          </div>
        </div>

        {/* Questions */}
        {visibleQuestions.length === 0 ? (
          // This case should ideally not happen due to activePages filtering, 
          // but keeping as safe fallback
          <div className="py-8 text-center text-black">
            <p>No questions to display on this page.</p>
          </div>
        ) : (
          <div className="space-y-6">
            {visibleQuestions.map((question, index) => (
              <QuestionRenderer
                key={question.id}
                question={question}
                questionNumber={getQuestionStartIndex(currentPageIndex) + index + 1}
                isSelf={isSelf}
                value={responses[question.id]}
                onChange={(value) => handleAnswerChange(question.id, value)}
                isPreview={isPreview}
              />
            ))}
          </div>
        )}
      </div>

      {/* Navigation */}
      <div className={showHeader ? "bg-white rounded-lg border border-gray-200 p-4" : "p-4"}>
        <div className="flex justify-between items-center gap-2">
          <button
            onClick={handlePrevious}
            disabled={currentPageIndex === 0}
            className="px-4 sm:px-6 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm sm:text-base whitespace-nowrap"
          >
            ← <span className="hidden sm:inline">Previous</span>
            <span className="sm:hidden">Prev</span>
          </button>

          {/* Page Indicators - Hidden on mobile, shown on desktop */}
          <div className="hidden sm:flex gap-2">
            {activePages.map((_, index) => {
              if (index < currentPageIndex - 2 || index > currentPageIndex + 2) return null;
              return (
                <button
                  key={index}
                  onClick={() => setCurrentPageIndex(index)}
                  className={`w-8 h-8 rounded-full transition-colors text-sm ${index === currentPageIndex
                    ? 'bg-blue-600 text-white'
                    : 'bg-gray-200 text-gray-600 hover:bg-gray-300'
                    }`}
                >
                  {index + 1}
                </button>
              );
            })}
          </div>

          {/* Page Counter for Mobile */}
          <div className="sm:hidden text-xs font-medium text-gray-500">
            Page {displayPageIndex} of {totalDisplayPages}
          </div>

          {currentPageIndex < activePages.length - 1 ? (
            <button
              onClick={handleNext}
              disabled={!isPageComplete()}
              className="px-4 sm:px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm sm:text-base whitespace-nowrap"
            >
              Next →
            </button>
          ) : (
            <button
              onClick={onSubmit}
              disabled={!isEverythingComplete() || isPreview}
              className="px-4 sm:px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm sm:text-base whitespace-nowrap"
            >
              {isPreview ? 'Preview' : 'Submit'}
            </button>
          )}
        </div>

        {(!isEverythingComplete() && !isPreview) && (
          <div className="mt-3 text-center">
            {/* {getIncompletePages().length > 0 && (
              <p className="text-sm text-red-600">
                Please answer all required questions on {getIncompletePages().length === 1
                  ? `Page ${getIncompletePages()[0]}`
                  : `Pages ${getIncompletePages().join(', ')}`} to continue
              </p>
            )} */}
          </div>
        )}
      </div>
    </div>
  );
}

