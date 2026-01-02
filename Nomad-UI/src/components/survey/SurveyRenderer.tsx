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

  const currentPage = survey.pages[currentPageIndex];
  const isSelf = relationshipType === 'Self';

  // Filter questions based on showTo and relationship
  const getVisibleQuestions = (questions: Question[]) => {
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

  const visibleQuestions = getVisibleQuestions(currentPage.questions);

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

    return visibleQuestions.every((q) => {
      if (!q.required) return true;
      const answer = responses[q.id];
      if (answer === undefined || answer === null || answer === '') return false;
      if (Array.isArray(answer) && answer.length === 0) return false;
      return true;
    });
  };

  // Navigation handlers
  const handleNext = () => {
    if (currentPageIndex < survey.pages.length - 1) {
      setCurrentPageIndex(currentPageIndex + 1);
    }
  };

  const handlePrevious = () => {
    if (currentPageIndex > 0) {
      setCurrentPageIndex(currentPageIndex - 1);
    }
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

      {/* Page Content */}
      <div className={showHeader ? "bg-white rounded-lg border border-gray-200 p-6 mb-6" : "mb-6"}>
        {/* Page Header */}
        <div className="mb-6">
          <h2 className="text-xl font-semibold text-gray-900">{currentPage.title}</h2>
          {currentPage.description && (
            <p className="text-gray-600 mt-2">{currentPage.description}</p>
          )}
          <div className="mt-3 text-sm text-gray-500">
            Page {currentPageIndex + 1} of {survey.pages.length}
          </div>
        </div>

        {/* Questions */}
        {visibleQuestions.length === 0 ? (
          <div className="py-8 text-center text-black">
            <p>No questions to display on this page.</p>
          </div>
        ) : (
          <div className="space-y-6">
            {visibleQuestions.map((question, index) => (
              <QuestionRenderer
                key={question.id}
                question={question}
                questionNumber={index + 1}
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
        <div className="flex justify-between items-center">
          <button
            onClick={handlePrevious}
            disabled={currentPageIndex === 0}
            className="px-6 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            ← Previous
          </button>

          {/* Page Indicators */}
          <div className="flex gap-2">
            {survey.pages.map((_, index) => (
              <button
                key={index}
                onClick={() => setCurrentPageIndex(index)}
                className={`w-8 h-8 rounded-full transition-colors ${index === currentPageIndex
                    ? 'bg-blue-600 text-white'
                    : 'bg-gray-200 text-gray-600 hover:bg-gray-300'
                  }`}
              >
                {index + 1}
              </button>
            ))}
          </div>

          {currentPageIndex < survey.pages.length - 1 ? (
            <button
              onClick={handleNext}
              disabled={!isPageComplete()}
              className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next →
            </button>
          ) : (
            <button
              onClick={onSubmit}
              disabled={!isPageComplete() || isPreview}
              className="px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isPreview ? 'Preview Mode' : 'Submit'}
            </button>
          )}
        </div>

        {!isPageComplete() && !isPreview && (
          <p className="text-sm text-red-600 mt-3 text-center">
            Please answer all required questions to continue
          </p>
        )}
      </div>
    </div>
  );
}

