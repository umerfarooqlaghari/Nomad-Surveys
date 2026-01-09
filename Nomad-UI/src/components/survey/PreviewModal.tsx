/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState } from 'react';
import { SurveySchema } from '@/types/survey';
import SurveyRenderer from './SurveyRenderer';

interface PreviewModalProps {
  survey: SurveySchema;
  onClose: () => void;
}

export default function PreviewModal({ survey, onClose }: PreviewModalProps) {
  const [previewMode, setPreviewMode] = useState<'self' | 'others'>('self');

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-transparent backdrop-blur-md border-4 border-black rounded-lg">
      <div className="flex items-center justify-center min-h-screen px-4 py-8">
        <div className="bg-white rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-hidden flex flex-col">
          {/* Header */}
          <div className="border-b border-gray-200 p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-2xl font-bold text-gray-900">Survey Preview</h2>
              <button
                onClick={onClose}
                className="text-gray-500 hover:text-gray-700"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {/* Preview Mode Toggle */}
            <div className="flex gap-2">
              <button
                onClick={() => setPreviewMode('self')}
                className={`px-4 py-2 rounded-lg font-medium transition-colors ${previewMode === 'self'
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                  }`}
              >
                Self Evaluation View
              </button>
              <button
                onClick={() => setPreviewMode('others')}
                className={`px-4 py-2 rounded-lg font-medium transition-colors ${previewMode === 'others'
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                  }`}
              >
                Others Evaluation View
              </button>
            </div>
          </div>

          {/* Preview Content */}
          <div className="flex-1 overflow-y-auto p-6 bg-gray-50">
            <SurveyRenderer
              survey={survey}
              relationshipType={previewMode === 'self' ? 'Self' : 'Manager'}
              isPreview={true}
            />
          </div>

          {/* Footer */}
          <div className="border-t border-gray-200 p-4 bg-gray-50">
            <button
              onClick={onClose}
              className="w-full py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            >
              Close Preview
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

