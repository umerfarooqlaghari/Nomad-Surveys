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
<div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8">
  <h2 className="text-2xl font-semibold text-gray-900 mb-6">Project Profile</h2>

  <div className="grid grid-cols-1 gap-2">
    {/* Basic Information */}
    <div className="space-y-5">
      <h3 className="text-lg font-semibold text-gray-800 border-b pb-2">Basic Information</h3>
      <div className="grid grid-cols-2 gap-y-6 text-sm">
        <div className="text-gray-500 font-medium">Project Name</div>
        <div className="text-gray-900 font-semibold">{projectData.name}</div>

        <div className="text-gray-500 font-medium">Project Slug</div>
        <div className="text-gray-900 font-semibold">{projectData.slug}</div>

        <div className="text-gray-500 font-medium">Organization</div>
        <div className="text-gray-900 font-semibold">{projectData.organizationName}</div>

        <div className="text-gray-500 font-medium">Admin Email</div>
        <div className="text-gray-900 font-semibold">{projectData.adminEmail}</div>

        <div className="text-gray-500 font-medium">Status</div>
        <div>
          <span className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold 
            ${projectData.status === 'Active' 
              ? 'bg-green-100 text-green-700' 
              : 'bg-gray-100 text-gray-700'}`}>
            {projectData.status}
          </span>
        </div>

        <div className="text-gray-500 font-medium">Created</div>
        <div className="text-gray-900 font-semibold">
          {new Date(projectData.createdAt).toLocaleDateString()}
        </div>
      </div>
    </div>
  </div>
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
