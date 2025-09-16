/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';

interface ProjectProfileTabProps {
  projectSlug: string;
}

export default function ProjectProfileTab({ projectSlug }: ProjectProfileTabProps) {
  const [projectData, setProjectData] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Simulate loading project data
    setTimeout(() => {
      setProjectData({
        name: projectSlug.replace('-', ' ').toUpperCase(),
        slug: projectSlug,
        description: 'This is a sample project description',
        organizationName: 'Sample Organization',
        adminEmail: 'admin@example.com',
        createdAt: new Date().toISOString(),
        status: 'Active',
        totalSubjects: 0,
        totalEvaluators: 0,
        totalSurveys: 0
      });
      setIsLoading(false);
    }, 1000);
  }, [projectSlug]);

  if (isLoading) {
    return (
      <div className="bg-white shadow rounded-lg p-6">
        <div className="animate-pulse">
          <div className="h-4 bg-gray-200 rounded w-1/4 mb-4"></div>
          <div className="h-4 bg-gray-200 rounded w-1/2 mb-2"></div>
          <div className="h-4 bg-gray-200 rounded w-1/3"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Project Overview */}
      <div className="bg-white shadow rounded-lg p-6">
        <h2 className="text-2xl font-bold text-gray-900 mb-4">
          Project Profile
        </h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4">Basic Information</h3>
            <dl className="space-y-3">
              <div>
                <dt className="text-sm font-medium text-gray-500">Project Name</dt>
                <dd className="text-sm text-gray-900">{projectData.name}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Project Slug</dt>
                <dd className="text-sm text-gray-900">{projectData.slug}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Organization</dt>
                <dd className="text-sm text-gray-900">{projectData.organizationName}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Admin Email</dt>
                <dd className="text-sm text-gray-900">{projectData.adminEmail}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Status</dt>
                <dd className="text-sm text-gray-900">
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                    {projectData.status}
                  </span>
                </dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Created</dt>
                <dd className="text-sm text-gray-900">
                  {new Date(projectData.createdAt).toLocaleDateString()}
                </dd>
              </div>
            </dl>
          </div>

          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4">Project Statistics</h3>
            <dl className="space-y-3">
              <div>
                <dt className="text-sm font-medium text-gray-500">Total Subjects</dt>
                <dd className="text-2xl font-bold text-blue-600">{projectData.totalSubjects}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Total Evaluators</dt>
                <dd className="text-2xl font-bold text-green-600">{projectData.totalEvaluators}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Total Surveys</dt>
                <dd className="text-2xl font-bold text-purple-600">{projectData.totalSurveys}</dd>
              </div>
            </dl>
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-white shadow rounded-lg p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Quick Actions</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <button className="bg-blue-50 hover:bg-blue-100 p-4 rounded-lg text-left transition-colors">
            <div className="text-blue-600 font-medium">Add Subjects</div>
            <div className="text-sm text-blue-500">Import or add new subjects</div>
          </button>
          <button className="bg-green-50 hover:bg-green-100 p-4 rounded-lg text-left transition-colors">
            <div className="text-green-600 font-medium">Add Evaluators</div>
            <div className="text-sm text-green-500">Import or add new evaluators</div>
          </button>
          <button className="bg-purple-50 hover:bg-purple-100 p-4 rounded-lg text-left transition-colors">
            <div className="text-purple-600 font-medium">Create Survey</div>
            <div className="text-sm text-purple-500">Build a new survey</div>
          </button>
        </div>
      </div>

      {/* Recent Activity */}
      <div className="bg-white shadow rounded-lg p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Recent Activity</h3>
        <div className="space-y-3">
          <div className="flex items-center space-x-3">
            <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
            <div className="text-sm text-gray-600">Project created</div>
            <div className="text-xs text-gray-400">
              {new Date(projectData.createdAt).toLocaleDateString()}
            </div>
          </div>
          <div className="flex items-center space-x-3">
            <div className="w-2 h-2 bg-gray-300 rounded-full"></div>
            <div className="text-sm text-gray-400">No additional activity yet</div>
          </div>
        </div>
      </div>
    </div>
  );
}
