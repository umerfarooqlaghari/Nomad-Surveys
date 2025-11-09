/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState } from 'react';
import { Question, QUESTION_TYPE_LABELS } from '@/types/survey';
import QuestionTypeConfigurator from './QuestionTypeConfigurator';

interface QuestionEditorProps {
  question: Question;
  questionNumber: number;
  totalQuestions: number;
  onUpdate: (updatedQuestion: Question) => void;
  onDelete: () => void;
  onMoveUp?: () => void;
  onMoveDown?: () => void;
}

export default function QuestionEditor({
  question,
  questionNumber,
  totalQuestions,
  onUpdate,
  onDelete,
  onMoveUp,
  onMoveDown,
}: QuestionEditorProps) {
  const [isExpanded, setIsExpanded] = useState(true);

  // Update question field
  const handleFieldChange = (field: keyof Question, value: any) => {
    onUpdate({ ...question, [field]: value });
  };

  return (
    <div className="border border-gray-300 rounded-lg bg-gray-50">
      {/* Question Header */}
      <div className="p-4 flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-2">
            <span className="text-sm font-medium text-gray-500">Q{questionNumber}</span>
            <span className="text-xs px-2 py-1 bg-blue-100 text-blue-700 rounded">
              {QUESTION_TYPE_LABELS[question.type]}
            </span>
            {question.required && (
              <span className="text-xs px-2 py-1 bg-red-100 text-red-700 rounded">Required</span>
            )}
            {question.importedFrom && (
              <span className="text-xs px-2 py-1 bg-green-100 text-green-700 rounded">
                Imported
              </span>
            )}
          </div>

          {/* Question Preview */}
          <div className="text-sm text-black">
            <p className="font-medium">
              Self: {question.selfText || <span className="text-gray-400 italic">No text</span>}
            </p>
            <p className="font-medium mt-1">
              Others: {question.othersText || <span className="text-gray-400 italic">No text</span>}
            </p>
          </div>
        </div>

        {/* Question Actions */}
        <div className="flex items-center gap-2 ml-4">
          <button
            onClick={() => setIsExpanded(!isExpanded)}
            className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-200 rounded"
            title={isExpanded ? 'Collapse' : 'Expand'}
          >
            <svg
              className={`w-5 h-5 transition-transform ${isExpanded ? 'rotate-180' : ''}`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>
          {onMoveUp && (
            <button
              onClick={onMoveUp}
              className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-200 rounded"
              title="Move up"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
              </svg>
            </button>
          )}
          {onMoveDown && (
            <button
              onClick={onMoveDown}
              className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-200 rounded"
              title="Move down"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </button>
          )}
          <button
            onClick={onDelete}
            className="p-2 text-red-500 hover:text-red-700 hover:bg-red-100 rounded"
            title="Delete question"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </div>

      {/* Question Editor (Expanded) */}
      {isExpanded && (
        <div className="border-t border-gray-300 p-4 bg-white">
          {/* Question Type Selector */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Question Type
            </label>
            <select
              value={question.type}
              onChange={(e) => handleFieldChange('type', e.target.value as Question['type'])}
              className="w-full px-3 py-2 text-black border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {Object.entries(QUESTION_TYPE_LABELS).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </div>

          {/* Self Question Text */}
          <div className="mb-4">
            <label className="block text-black font-medium text-black mb-2">
              Self Question Text *
            </label>
            <textarea
              value={question.selfText}
              onChange={(e) => handleFieldChange('selfText', e.target.value)}
              placeholder="Question text shown to self-evaluators..."
              rows={2}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-400"
            />
          </div>

          {/* Others Question Text */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-black mb-2">
              Others Question Text *
            </label>
            <textarea
              value={question.othersText}
              onChange={(e) => handleFieldChange('othersText', e.target.value)}
              placeholder="Question text shown to others (managers, peers, etc.)..."
              rows={2}
              className="w-full px-3 py-2  text-black border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-400"
            />
          </div>

          {/* Description */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-black mb-2">
              Description (Optional)
            </label>
            <textarea
              value={question.description || ''}
              onChange={(e) => handleFieldChange('description', e.target.value)}
              placeholder="Additional context or instructions..."
              rows={2}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-400"
            />
          </div>

          {/* Required Toggle */}
          <div className="mb-4">
            <div
              className="flex items-center gap-3 cursor-pointer select-none"
              onClick={() => handleFieldChange('required', !question.required)}
            >
              <input
                type="checkbox"
                checked={question.required}
                // onChange={() => {}}
                      onChange={() => handleFieldChange('required', !question.required)}
                onClick={(e) => e.stopPropagation()}
                className="w-5 h-5 relative z-10 text-blue-600 border-gray-300 rounded focus:ring-blue-500 cursor-pointer"
                style={{ pointerEvents: 'auto' }}
              />
              <span className="text-sm font-medium text-black">Required Question</span>
            </div>
          </div>
      {/* <div className="mb-4">
  <label className="flex items-center gap-3 cursor-pointer select-none">
      onClick={(e) => handleFieldChange('false', e.target.checked)}
    <input
      type="checkbox"
      checked={question.required}
      onChange={(e) => handleFieldChange('required', e.target.checked)}
      className="w-5 h-5 text-blue-600 border-gray-300 rounded focus:ring-blue-500 cursor-pointer"
      onClick={(e) => e.stopPropagation()}
    />
    <span className="text-sm font-medium text-black">Required Question</span>
  </label>
</div> */}



          {/* Question Type Configuration */}
          <QuestionTypeConfigurator
            question={question}
            onUpdate={onUpdate}
          />
        </div>
      )}
    </div>
  );
}

