'use client';

import React from 'react';

interface ProjectAnalyticsTabProps {
  projectSlug: string;
}

export default function ProjectAnalyticsTab({ projectSlug }: ProjectAnalyticsTabProps) {
  return (
    <div className="space-y-6">
      <div className="bg-white shadow rounded-lg p-6">
        <h2 className="text-2xl font-bold text-gray-900 mb-4">
          Analytics Dashboard
        </h2>
        <p className="text-gray-600 mb-6">
          Project: {projectSlug}
        </p>
        
        {/* Analytics Cards */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
          <div className="bg-blue-50 p-6 rounded-lg">
            <h3 className="text-lg font-semibold text-blue-900 mb-2">Total Surveys</h3>
            <p className="text-3xl font-bold text-blue-600">0</p>
            <p className="text-sm text-blue-500">Active surveys</p>
          </div>
          
          <div className="bg-green-50 p-6 rounded-lg">
            <h3 className="text-lg font-semibold text-green-900 mb-2">Responses</h3>
            <p className="text-3xl font-bold text-green-600">0</p>
            <p className="text-sm text-green-500">Total responses</p>
          </div>
          
          <div className="bg-yellow-50 p-6 rounded-lg">
            <h3 className="text-lg font-semibold text-yellow-900 mb-2">Completion Rate</h3>
            <p className="text-3xl font-bold text-yellow-600">0%</p>
            <p className="text-sm text-yellow-500">Average completion</p>
          </div>
          
          <div className="bg-purple-50 p-6 rounded-lg">
            <h3 className="text-lg font-semibold text-purple-900 mb-2">Participants</h3>
            <p className="text-3xl font-bold text-purple-600">0</p>
            <p className="text-sm text-purple-500">Active participants</p>
          </div>
        </div>

        {/* Charts Placeholder */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="bg-gray-50 p-6 rounded-lg">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Survey Completion Trends</h3>
            <div className="h-64 flex items-center justify-center text-gray-500">
              Chart placeholder - Survey completion over time
            </div>
          </div>
          
          <div className="bg-gray-50 p-6 rounded-lg">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Response Distribution</h3>
            <div className="h-64 flex items-center justify-center text-gray-500">
              Chart placeholder - Response distribution by department
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
