/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React from 'react';
import { Question } from '@/types/survey';

interface QuestionRendererProps {
  question: Question;
  questionNumber: number;
  isSelf: boolean;
  value: any;
  onChange: (value: any) => void;
  isPreview?: boolean;
}

export default function QuestionRenderer({
  question,
  questionNumber,
  isSelf,
  value,
  onChange,
  isPreview = false,
}: QuestionRendererProps) {
  const questionText = isSelf ? question.selfText : question.othersText;

  // Rating Scale Renderer
  const renderRating = () => {
    // Use custom rating options if defined, otherwise fall back to min/max/step
    const customOptions = question.config.ratingOptions;

    if (customOptions && customOptions.length > 0) {
      // Custom rating options (e.g., "Happy", "Sad", "Neutral" or "100", "200", "500")
      return (
        <div className="space-y-3">
          <div className="flex flex-wrap gap-2">
            {customOptions.map((option) => (
              <button
                key={option.id}
                onClick={() => onChange(option.text)}
                disabled={isPreview}
                className={`py-3 px-6 border-2 rounded-lg font-medium transition-colors ${
                  value === option.text
                    ? 'border-blue-600 bg-blue-50 text-blue-700'
                    : 'border-gray-300 bg-white text-gray-700 hover:border-blue-400'
                } ${isPreview ? 'cursor-default' : 'cursor-pointer'}`}
              >
                {option.text}
              </button>
            ))}
          </div>
        </div>
      );
    } else {
      // Numeric rating scale with min/max/step
      const min = question.config.ratingMin || 1;
      const max = question.config.ratingMax || 5;
      const step = question.config.ratingStep || 1;
      const labels = question.config.ratingLabels;

      const options = [];
      for (let i = min; i <= max; i += step) {
        options.push(i);
      }

      return (
        <div className="space-y-3">
          <div className="flex justify-between items-center gap-2">
            {options.map((option) => (
              <button
                key={option}
                onClick={() => onChange(option)}
                disabled={isPreview}
                className={`flex-1 py-3 px-4 border-2 rounded-lg font-medium transition-colors ${
                  value === option
                    ? 'border-blue-600 bg-blue-50 text-blue-700'
                    : 'border-gray-300 bg-white text-gray-700 hover:border-blue-400'
                } ${isPreview ? 'cursor-default' : 'cursor-pointer'}`}
              >
                {option}
              </button>
            ))}
          </div>
          {labels && (
            <div className="flex justify-between text-sm text-gray-600">
              <span>{labels.min}</span>
              <span>{labels.max}</span>
            </div>
          )}
        </div>
      );
    }
  };

  // Single Choice Renderer
  const renderSingleChoice = () => {
    const options = question.config.options || [];

    return (
      <div className="space-y-2">
        {options.map((option) => (
          <label
            key={option.id}
            htmlFor={`q-${question.id}-opt-${option.id}`}
            className={`flex items-center gap-3 p-3 border-2 rounded-lg cursor-pointer transition-colors ${
              value === option.id
                ? 'border-blue-600 bg-blue-50'
                : 'border-gray-300 bg-white hover:border-blue-400'
            } ${isPreview ? 'cursor-default' : ''}`}
          >
            <input
                id={`q-${question.id}-opt-${option.id}`}
              type="radio"
              name={question.id}
              value={option.id}
              checked={value === option.id}
              onChange={() => onChange(option.id)}
              disabled={isPreview}
              className="w-4 h-4 text-blue-600 border-gray-300 focus:ring-blue-500"
            />
            <span className="text-gray-900">{option.text}</span>
          </label>
        ))}
      </div>
    );
  };

  // Multiple Choice Renderer
  const renderMultipleChoice = () => {
    const options = question.config.options || [];
    const selectedValues = Array.isArray(value) ? value : [];

    const handleToggle = (optionId: string) => {
      if (selectedValues.includes(optionId)) {
        onChange(selectedValues.filter((id: string) => id !== optionId));
      } else {
        onChange([...selectedValues, optionId]);
      }
    };

    return (
      <div className="space-y-2">
        {options.map((option) => (
          <label
            key={option.id}
            onClick={() => handleToggle(option.id)}
            className={`flex items-center gap-3 p-3 border-2 rounded-lg transition-colors ${
              selectedValues.includes(option.id)
                ? 'border-blue-600 bg-blue-50'
                : 'border-gray-300 bg-white hover:border-blue-400'
            } ${isPreview ? 'cursor-default' : ''}`}
          >
            <input
              type="checkbox"
              checked={selectedValues.includes(option.id)}
              onChange={() => handleToggle(option.id)}
              disabled={isPreview}
              className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500 cursor-pointer"
            />
            <span className="text-gray-900">{option.text}</span>
          </label>
        ))}
        {question.config.minSelections !== undefined && question.config.minSelections > 0 && (
          <p className="text-sm text-gray-600 mt-2">
            Select at least {question.config.minSelections} option(s)
          </p>
        )}
        {question.config.maxSelections !== undefined && question.config.maxSelections > 0 && (
          <p className="text-sm text-gray-600 mt-2">
            Select up to {question.config.maxSelections} option(s)
          </p>
        )}
      </div>
    );
  };

  // Text Input Renderer
  const renderText = () => {
    return (
      <input
        type="text"
        value={value || ''}
        onChange={(e) => onChange(e.target.value)}
        placeholder={question.config.placeholder || 'Enter your answer...'}
        maxLength={question.config.maxLength}
        disabled={isPreview}
        className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black"
      />
    );
  };

  // Textarea Renderer
  const renderTextarea = () => {
    return (
      <div>
        <textarea
          value={value || ''}
          onChange={(e) => onChange(e.target.value)}
          placeholder={question.config.placeholder || 'Enter your answer...'}
          maxLength={question.config.maxLength}
          disabled={isPreview}
          rows={4}
          className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black"
        />
        {question.config.maxLength && (
          <p className="text-sm text-gray-500 mt-1 text-right">
            {(value || '').length} / {question.config.maxLength}
          </p>
        )}
      </div>
    );
  };

  // Dropdown Renderer
  const renderDropdown = () => {
    const options = question.config.options || [];

    return (
      <select
        value={value || ''}
        onChange={(e) => onChange(e.target.value)}
        disabled={isPreview}
        className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="">-- Select an option --</option>
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {option.text}
          </option>
        ))}
      </select>
    );
  };

  // Yes/No Renderer
  const renderYesNo = () => {
    return (
      <div className="flex gap-4">
        <button
          onClick={() => onChange('yes')}
          disabled={isPreview}
          className={`flex-1 py-3 px-6 border-2 rounded-lg font-medium transition-colors ${
            value === 'yes'
              ? 'border-green-600 bg-green-50 text-green-700'
              : 'border-gray-300 bg-white text-gray-700 hover:border-green-400'
          } ${isPreview ? 'cursor-default' : 'cursor-pointer'}`}
        >
          Yes
        </button>
        <button
          onClick={() => onChange('no')}
          disabled={isPreview}
          className={`flex-1 py-3 px-6 border-2 rounded-lg font-medium transition-colors ${
            value === 'no'
              ? 'border-red-600 bg-red-50 text-red-700'
              : 'border-gray-300 bg-white text-gray-700 hover:border-red-400'
          } ${isPreview ? 'cursor-default' : 'cursor-pointer'}`}
        >
          No
        </button>
      </div>
    );
  };

  // Date Renderer
  const renderDate = () => {
    return (
      <input
        type="date"
        value={value || ''}
        onChange={(e) => onChange(e.target.value)}
        disabled={isPreview}
        className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
    );
  };

  // Number Renderer
  const renderNumber = () => {
    return (
      <input
        type="number"
        value={value || ''}
        onChange={(e) => onChange(e.target.value)}
        min={question.config.numberMin}
        max={question.config.numberMax}
        disabled={isPreview}
        className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
    );
  };

  // Render appropriate input based on question type
  const renderInput = () => {
    switch (question.type) {
      case 'rating':
        return renderRating();
      case 'single-choice':
        return renderSingleChoice();
      case 'multiple-choice':
        return renderMultipleChoice();
      case 'text':
        return renderText();
      case 'textarea':
        return renderTextarea();
      case 'dropdown':
        return renderDropdown();
      case 'yes-no':
        return renderYesNo();
      case 'date':
        return renderDate();
      case 'number':
        return renderNumber();
      default:
        return <p className="text-gray-500">Unsupported question type</p>;
    }
  };

  return (
    <div className="border-b border-gray-200 pb-6 last:border-b-0">
      <div className="mb-3">
        <h3 className="text-lg font-medium text-gray-900">
          {questionNumber}. {questionText}
          {question.required && <span className="text-red-600 ml-1">*</span>}
        </h3>
        {question.description && (
          <p className="text-sm text-gray-600 mt-1">{question.description}</p>
        )}
      </div>
      {renderInput()}
    </div>
  );
}

