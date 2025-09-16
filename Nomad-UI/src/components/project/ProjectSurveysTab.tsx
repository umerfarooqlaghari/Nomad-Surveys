/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState } from 'react';

interface ProjectSurveysTabProps {
  projectSlug: string;
}

export default function ProjectSurveysTab({ projectSlug }: ProjectSurveysTabProps) {
  const [surveys, setSurveys] = useState<any[]>([]);

  return (
    <div className="space-y-6">
      <div className="bg-white shadow rounded-lg p-6">
        <div className="flex justify-between items-center mb-4">
          <div>
            <h2 className="text-2xl font-bold text-gray-900">Surveys</h2>
            <p className="text-gray-600">Manage surveys for project: {projectSlug}</p>
          </div>
          <div className="flex space-x-3">
            <button className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium">
              Create Survey
            </button>
            <button className="bg-green-500 hover:bg-green-700 text-white px-4 py-2 rounded-md text-sm font-medium">
              Import Survey
            </button>
          </div>
        </div>

        {/* Survey Templates */}
        <div className="mb-8">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Survey Templates</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
              <h4 className="font-medium text-gray-900 mb-2">360 Feedback Survey</h4>
              <p className="text-sm text-gray-600 mb-3">Comprehensive 360-degree feedback assessment</p>
              <button className="bg-blue-500 hover:bg-blue-700 text-white px-3 py-1 rounded text-sm">
                Use Template
              </button>
            </div>
            
            <div className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
              <h4 className="font-medium text-gray-900 mb-2">Employee Satisfaction</h4>
              <p className="text-sm text-gray-600 mb-3">Measure employee satisfaction and engagement</p>
              <button className="bg-blue-500 hover:bg-blue-700 text-white px-3 py-1 rounded text-sm">
                Use Template
              </button>
            </div>
            
            <div className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
              <h4 className="font-medium text-gray-900 mb-2">Performance Review</h4>
              <p className="text-sm text-gray-600 mb-3">Annual performance review questionnaire</p>
              <button className="bg-blue-500 hover:bg-blue-700 text-white px-3 py-1 rounded text-sm">
                Use Template
              </button>
            </div>
          </div>
        </div>

        {/* Active Surveys */}
        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4">Active Surveys</h3>
          {surveys.length === 0 ? (
            <div className="bg-gray-50 rounded-lg p-8 text-center">
              <p className="text-gray-500 mb-4">No surveys created yet</p>
              <button className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm">
                Create Your First Survey
              </button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Survey Name
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Type
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Responses
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Created
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {surveys.map((survey) => (
                    <tr key={survey.id}>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="text-sm font-medium text-gray-900">{survey.name}</div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {survey.type}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                          {survey.status}
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {survey.responses}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {survey.createdAt}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                        <button className="text-blue-600 hover:text-blue-900 mr-3">
                          Edit
                        </button>
                        <button className="text-green-600 hover:text-green-900 mr-3">
                          View
                        </button>
                        <button className="text-red-600 hover:text-red-900">
                          Delete
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
