/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React from 'react';
import { Question, ChoiceOption, generateOptionId } from '@/types/survey';

interface QuestionTypeConfiguratorProps {
  question: Question;
  onUpdate: (updatedQuestion: Question) => void;
}

export default function QuestionTypeConfigurator({
  question,
  onUpdate,
}: QuestionTypeConfiguratorProps) {
  const updateConfig = (updates: Partial<Question['config']>) => {
    onUpdate({
      ...question,
      config: { ...question.config, ...updates },
    });
  };

  // Rating Scale Configuration
  if (question.type === 'rating') {
    const ratingOptions = question.config.ratingOptions || [];

    const addRatingOption = () => {
      const newOption: ChoiceOption = {
        id: generateOptionId(),
        text: '',
        value: ratingOptions.length > 0 ? (ratingOptions[ratingOptions.length - 1].score ?? ratingOptions.length) + 1 : 1, // Use score as value
        order: ratingOptions.length,
        score: ratingOptions.length > 0 ? (ratingOptions[ratingOptions.length - 1].score ?? ratingOptions.length) + 1 : 1,
      };
      updateConfig({ ratingOptions: [...ratingOptions, newOption] });
    };

    const updateRatingOption = (index: number, text: string) => {
      const updated = [...ratingOptions];
      updated[index] = { ...updated[index], text };
      updateConfig({ ratingOptions: updated });
    };

    const deleteRatingOption = (index: number) => {
      const updated = ratingOptions.filter((_, i) => i !== index);
      // Reorder
      updated.forEach((opt, i) => {
        opt.order = i;
      });
      updateConfig({ ratingOptions: updated });
    };

    const moveRatingOption = (index: number, direction: 'up' | 'down') => {
      const newIndex = direction === 'up' ? index - 1 : index + 1;
      if (newIndex < 0 || newIndex >= ratingOptions.length) return;

      const updated = [...ratingOptions];
      [updated[index], updated[newIndex]] = [updated[newIndex], updated[index]];
      // Reorder
      updated.forEach((opt, i) => {
        opt.order = i;
      });
      updateConfig({ ratingOptions: updated });
    };

    return (
      <div className="border-t border-gray-200 pt-4">
        <h4 className="text-sm font-semibold text-black mb-3">Rating Scale Settings</h4>

        <div className="mb-4">
          <div className="flex items-center justify-between mb-2">
            <label className="block text-sm font-medium text-black">
              Rating Options
            </label>
            <button
              type="button"
              onClick={addRatingOption}
              className="px-3 py-1 text-sm bg-blue-600 text-white rounded hover:bg-blue-700"
            >
              + Add Option
            </button>
          </div>
          <p className="text-xs text-gray-600 mb-3">
            Define custom rating options with their corresponding numerical scores
          </p>

          {ratingOptions.length === 0 ? (
            <div className="text-sm text-black italic p-4 border border-dashed border-gray-300 rounded-lg text-center">
              No rating options defined. Click &quot;Add Option&quot; to create custom rating scale.
            </div>
          ) : (
            <div className="space-y-2">
              <div className="flex px-2 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                <div className="w-8">#</div>
                <div className="flex-1">Text</div>
                <div className="w-20 mx-2">Score</div>
                <div className="w-24 text-right">Actions</div>
              </div>
              {ratingOptions.map((option, index) => (
                <div key={option.id} className="flex items-center gap-2">
                  <span className="text-sm font-medium text-gray-600 w-8">{index + 1}.</span>
                  <input
                    type="text"
                    value={option.text}
                    onChange={(e) => updateRatingOption(index, e.target.value)}
                    placeholder={`Option ${index + 1}`}
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-400"
                  />
                  <input
                    type="number"
                    value={option.score ?? index + 1}
                    onChange={(e) => {
                      const updated = [...ratingOptions];
                      const newScore = parseInt(e.target.value) || 0;
                      updated[index] = { ...updated[index], score: newScore, value: newScore }; // Sync value with score
                      updateConfig({ ratingOptions: updated });
                    }}
                    className="w-20 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black text-center"
                    title="Score value"
                  />
                  <div className="flex items-center justify-end w-24">
                    <button
                      type="button"
                      onClick={() => moveRatingOption(index, 'up')}
                      disabled={index === 0}
                      className="p-1 text-gray-500 hover:text-gray-700 disabled:opacity-30"
                      title="Move up"
                    >
                      ↑
                    </button>
                    <button
                      type="button"
                      onClick={() => moveRatingOption(index, 'down')}
                      disabled={index === ratingOptions.length - 1}
                      className="p-1 text-gray-500 hover:text-gray-700 disabled:opacity-30"
                      title="Move down"
                    >
                      ↓
                    </button>
                    <button
                      type="button"
                      onClick={() => deleteRatingOption(index)}
                      className="p-1 text-red-500 hover:text-red-700 ml-1"
                      title="Delete"
                    >
                      ✕
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    );
  }

  // Multiple Choice / Dropdown Configuration
  if (['single-choice', 'multiple-choice', 'dropdown'].includes(question.type)) {
    const options = question.config.options || [];

    const addOption = () => {
      const newOption: ChoiceOption = {
        id: generateOptionId(),
        text: `Option ${options.length + 1}`,
        value: options.length > 0 ? (options[options.length - 1].score ?? options.length) + 1 : 1, // Use score as value
        order: options.length,
        score: options.length > 0 ? (options[options.length - 1].score ?? options.length) + 1 : 1,
      };
      updateConfig({ options: [...options, newOption] });
    };

    const updateOption = (optionId: string, text: string) => {
      updateConfig({
        options: options.map((opt) =>
          opt.id === optionId ? { ...opt, text } : opt
        ),
      });
    };

    const deleteOption = (optionId: string) => {
      if (options.length <= 2) {
        alert('You must have at least 2 options');
        return;
      }
      updateConfig({
        options: options
          .filter((opt) => opt.id !== optionId)
          .map((opt, index) => ({ ...opt, order: index })),
      });
    };

    const moveOption = (index: number, direction: 'up' | 'down') => {
      const newOptions = [...options];
      const targetIndex = direction === 'up' ? index - 1 : index + 1;
      [newOptions[index], newOptions[targetIndex]] = [newOptions[targetIndex], newOptions[index]];
      updateConfig({
        options: newOptions.map((opt, i) => ({ ...opt, order: i })),
      });
    };

    return (
      <div className="border-t border-gray-200 pt-4">
        <h4 className="text-sm font-semibold text-gray-700 mb-3">
          {question.type === 'dropdown' ? 'Dropdown' : 'Multiple Choice'} Options
        </h4>

        {/* Multiple Choice Specific Settings */}
        {question.type === 'multiple-choice' && (
          <div className="grid grid-cols-2 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Min Selections
              </label>
              <input
                type="number"
                value={question.config.minSelections || 0}
                onChange={(e) => updateConfig({ minSelections: parseInt(e.target.value) })}
                min={0}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-500 mb-1">
                Max Selections (0 = unlimited)
              </label>
              <input
                type="number"
                value={question.config.maxSelections || 0}
                onChange={(e) => updateConfig({ maxSelections: parseInt(e.target.value) || undefined })}
                min={0}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-500"
              />
            </div>
          </div>
        )}

        {/* Options List */}
        <div className="space-y-2 mb-3">
          <div className="flex px-2 text-xs font-semibold text-gray-500 uppercase tracking-wider">
            <div className="w-6">#</div>
            <div className="flex-1">Text</div>
            <div className="w-20 mx-2">Score</div>
            <div className="w-24 text-right">Actions</div>
          </div>
          {options.map((option, index) => (
            <div key={option.id} className="flex items-center gap-2">
              <span className="text-sm text-gray-500 w-6">{index + 1}.</span>
              <input
                type="text"
                value={option.text}
                onChange={(e) => updateOption(option.id, e.target.value)}
                className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-500"
              />
              <input
                type="number"
                value={option.score ?? index + 1}
                onChange={(e) => {
                  const newScore = parseInt(e.target.value) || 0;
                  updateConfig({
                    options: options.map((opt) =>
                      opt.id === option.id ? { ...opt, score: newScore, value: newScore } : opt // Sync value with score
                    ),
                  });
                }}
                className="w-20 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-500 text-center"
                title="Score value"
              />
              <div className="flex items-center justify-end w-24">
                <button
                  onClick={() => moveOption(index, 'up')}
                  disabled={index === 0}
                  className="p-1 text-gray-500 hover:text-gray-700 disabled:opacity-30"
                >
                  ↑
                </button>
                <button
                  onClick={() => moveOption(index, 'down')}
                  disabled={index === options.length - 1}
                  className="p-1 text-gray-500 hover:text-gray-700 disabled:opacity-30"
                >
                  ↓
                </button>
                <button
                  onClick={() => deleteOption(option.id)}
                  className="p-1 text-red-500 hover:text-red-700 ml-1"
                >
                  ✕
                </button>
              </div>
            </div>
          ))}
        </div>

        <button
          onClick={addOption}
          className="w-full py-2 border border-dashed border-gray-300 rounded-lg text-white hover:border-blue-500 hover:text-blue-600 transition-colors"
        >
          + Add Option
        </button>
      </div>
    );
  }

  // Text / Textarea Configuration
  if (['text', 'textarea'].includes(question.type)) {
    return (
      <div className="border-t border-gray-200 pt-4">
        <h4 className="text-sm font-semibold text-gray-700 mb-3">Text Input Settings</h4>

        <div className="grid grid-cols-2 gap-4 mb-4">
          <div>
            <label className="block text-sm font-medium text-gray-500 mb-1">
              Min Length
            </label>
            <input
              type="number"
              value={question.config.minLength || 0}
              onChange={(e) => updateConfig({ minLength: parseInt(e.target.value) })}
              min={0}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Max Length
            </label>
            <input
              type="number"
              value={question.config.maxLength || 500}
              onChange={(e) => updateConfig({ maxLength: parseInt(e.target.value) })}
              min={1}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-500"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Placeholder
          </label>
          <input
            type="text"
            value={question.config.placeholder || ''}
            onChange={(e) => updateConfig({ placeholder: e.target.value })}
            placeholder="Enter placeholder text..."
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-500"
          />
        </div>
      </div>
    );
  }





  return null;
}

