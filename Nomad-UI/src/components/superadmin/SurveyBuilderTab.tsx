'use client';

import React, { useState } from 'react';

export default function SurveyBuilderTab() {
  const [showCreateForm, setShowCreateForm] = useState(false);

  // Mock surveys data
  const surveys = [
    {
      id: 1,
      title: 'Employee Satisfaction Survey 2024',
      description: 'Annual employee satisfaction and engagement survey',
      status: 'Active',
      questions: 25,
      responses: 156,
      createdDate: '2024-01-15',
      lastModified: '2024-02-01',
      category: 'HR',
    },
    {
      id: 2,
      title: 'Leadership Assessment',
      description: '360-degree leadership evaluation survey',
      status: 'Draft',
      questions: 40,
      responses: 0,
      createdDate: '2024-02-10',
      lastModified: '2024-02-15',
      category: 'Leadership',
    },
    {
      id: 3,
      title: 'Customer Feedback Survey',
      description: 'Quarterly customer satisfaction survey',
      status: 'Completed',
      questions: 15,
      responses: 89,
      createdDate: '2024-01-01',
      lastModified: '2024-01-30',
      category: 'Customer Service',
    },
  ];

  const questionTypes = [
    { id: 'multiple-choice', name: 'Multiple Choice', icon: '🔘' },
    { id: 'text', name: 'Text Input', icon: '📝' },
    { id: 'rating', name: 'Rating Scale', icon: '⭐' },
    { id: 'yes-no', name: 'Yes/No', icon: '✅' },
    { id: 'dropdown', name: 'Dropdown', icon: '📋' },
    { id: 'matrix', name: 'Matrix/Grid', icon: '📊' },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Survey Builder</h2>
          <p className="text-sm text-gray-600">Create and manage surveys for evaluation</p>
        </div>
        <button
          onClick={() => setShowCreateForm(!showCreateForm)}
          className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
        >
          {showCreateForm ? 'Cancel' : '+ Create New Survey'}
        </button>
      </div>

      {/* Create Survey Form */}
      {showCreateForm && (
        <div className="bg-white shadow rounded-lg p-6">
          <h3 className="text-lg font-medium text-gray-900 mb-6">Create New Survey</h3>
          <div className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Survey Title *
                </label>
                <input
                  type="text"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Enter survey title"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Category *
                </label>
                <select className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option value="">Select Category</option>
                  <option value="hr">HR & Employee Relations</option>
                  <option value="leadership">Leadership Assessment</option>
                  <option value="performance">Performance Review</option>
                  <option value="customer">Customer Service</option>
                  <option value="training">Training & Development</option>
                  <option value="other">Other</option>
                </select>
              </div>
            </div>
            
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Description
              </label>
              <textarea
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Brief description of the survey purpose and scope"
              />
            </div>

            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setShowCreateForm(false)}
                className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
              >
                Cancel
              </button>
              <button className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">
                Create Survey
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Question Types Reference */}
      <div className="bg-white shadow rounded-lg p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Available Question Types</h3>
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
          {questionTypes.map((type) => (
            <div key={type.id} className="text-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50">
              <div className="text-2xl mb-2">{type.icon}</div>
              <div className="text-sm font-medium text-gray-900">{type.name}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Survey Statistics */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="bg-white shadow rounded-lg p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <span className="text-2xl">📝</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Total Surveys</p>
              <p className="text-2xl font-semibold text-gray-900">{surveys.length}</p>
            </div>
          </div>
        </div>
        <div className="bg-white shadow rounded-lg p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <span className="text-2xl">✅</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Active Surveys</p>
              <p className="text-2xl font-semibold text-gray-900">
                {surveys.filter(s => s.status === 'Active').length}
              </p>
            </div>
          </div>
        </div>
        <div className="bg-white shadow rounded-lg p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <span className="text-2xl">📊</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Total Responses</p>
              <p className="text-2xl font-semibold text-gray-900">
                {surveys.reduce((sum, s) => sum + s.responses, 0)}
              </p>
            </div>
          </div>
        </div>
        <div className="bg-white shadow rounded-lg p-6">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <span className="text-2xl">📈</span>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500">Avg. Response Rate</p>
              <p className="text-2xl font-semibold text-gray-900">73%</p>
            </div>
          </div>
        </div>
      </div>

      {/* Surveys List */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Survey List</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Survey
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Category
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Questions
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Responses
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Last Modified
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
                    <div>
                      <div className="text-sm font-medium text-gray-900">{survey.title}</div>
                      <div className="text-sm text-gray-500">{survey.description}</div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {survey.category}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    <span className="bg-blue-100 text-blue-800 px-2 py-1 rounded-full text-xs font-medium">
                      {survey.questions}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    <span className="bg-green-100 text-green-800 px-2 py-1 rounded-full text-xs font-medium">
                      {survey.responses}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      survey.status === 'Active' 
                        ? 'bg-green-100 text-green-800'
                        : survey.status === 'Draft'
                        ? 'bg-yellow-100 text-yellow-800'
                        : 'bg-gray-100 text-gray-800'
                    }`}>
                      {survey.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {survey.lastModified}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <button className="text-blue-600 hover:text-blue-900 mr-3">Edit</button>
                    <button className="text-green-600 hover:text-green-900 mr-3">Preview</button>
                    <button className="text-purple-600 hover:text-purple-900 mr-3">Duplicate</button>
                    <button className="text-red-600 hover:text-red-900">Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Survey Builder Placeholder */}
      <div className="bg-white shadow rounded-lg p-6">
        <div className="text-center py-12">
          <div className="text-6xl mb-4">🚧</div>
          <h3 className="text-lg font-medium text-gray-900 mb-2">Survey Builder Coming Soon</h3>
          <p className="text-gray-500 mb-4">
            Advanced drag-and-drop survey builder with question logic and branching
          </p>
          <div className="text-sm text-gray-400">
            Features will include: Question templates, Logic branching, Custom themes, Real-time preview
          </div>
        </div>
      </div>
    </div>
  );
}
