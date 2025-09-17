'use client';

import React from 'react';

interface ProjectReportsTabProps {
  projectSlug: string;
}

export default function ProjectReportsTab({ projectSlug }: ProjectReportsTabProps) {
  return (
    <div className="space-y-6">
      <div className="bg-white shadow rounded-lg p-6">
        <h2 className="text-2xl font-bold text-gray-900 mb-4">
          Reports
        </h2>
        <p className="text-gray-600 mb-6">
          Generate and download reports for project: {projectSlug}
        </p>
        
        {/* Report Types */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div className="border border-gray-200 rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-2">Survey Results Report</h3>
            <p className="text-gray-600 mb-4">Comprehensive survey results and analytics</p>
            <button className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm">
              Generate Report
            </button>
          </div>
          
          <div className="border border-gray-200 rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-2">Participant Report</h3>
            <p className="text-gray-600 mb-4">List of all participants and their status</p>
            <button className="bg-green-500 hover:bg-green-700 text-white px-4 py-2 rounded-md text-sm">
              Generate Report
            </button>
          </div>
          
          <div className="border border-gray-200 rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-2">Evaluator Report</h3>
            <p className="text-gray-600 mb-4">List of all evaluators and their assignments</p>
            <button className="bg-purple-500 hover:bg-purple-700 text-white px-4 py-2 rounded-md text-sm">
              Generate Report
            </button>
          </div>
          
          <div className="border border-gray-200 rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-2">360 Feedback Report</h3>
            <p className="text-gray-600 mb-4">360-degree feedback analysis and insights</p>
            <button className="bg-orange-500 hover:bg-orange-700 text-white px-4 py-2 rounded-md text-sm">
              Generate Report
            </button>
          </div>
          
          <div className="border border-gray-200 rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-2">Custom Report</h3>
            <p className="text-gray-600 mb-4">Create custom reports with specific filters</p>
            <button className="bg-gray-500 hover:bg-gray-700 text-white px-4 py-2 rounded-md text-sm">
              Create Custom
            </button>
          </div>
          
          <div className="border border-gray-200 rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-2">Export Data</h3>
            <p className="text-gray-600 mb-4">Export raw data in various formats</p>
            <button className="bg-red-500 hover:bg-red-700 text-white px-4 py-2 rounded-md text-sm">
              Export Data
            </button>
          </div>
        </div>

        {/* Recent Reports */}
        <div className="mt-8">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Recent Reports</h3>
          <div className="bg-gray-50 rounded-lg p-4">
            <p className="text-gray-500 text-center">No reports generated yet</p>
          </div>
        </div>
      </div>
    </div>
  );
}
