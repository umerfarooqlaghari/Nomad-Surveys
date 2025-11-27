/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { useParams } from 'next/navigation';
import Image from 'next/image';
import { useAuth } from '@/contexts/AuthContext';
import ProtectedRoute from '@/components/ProtectedRoute';
import ProjectProfileTab from '@/components/project/ProjectProfileTab';
import ProjectAnalyticsTab from '@/components/project/ProjectAnalyticsTab';
import ProjectReportsTab from '@/components/project/ProjectReportsTab';
import ProjectParticipantsTab from '@/components/project/ProjectParticipantsTab';
import ProjectEmployeesTab from '@/components/project/ProjectEmployeesTab';
import ProjectSurveysTab from '@/components/project/ProjectSurveysTab';
import ProjectQuestionsTab from '@/components/project/ProjectQuestionsTab';

const tabs = [
  { id: 'profile', name: 'Profile', icon: "/Icons/people.svg" },
  { id: 'questions', name: 'Questions', icon: "/Icons/ui-checks.svg" },
  { id: 'surveys', name: 'Surveys', icon: "/Icons/wrench-adjustable-circle.svg" },
  { id: 'participants', name: 'Participants', icon: "/Icons/person-add.svg" },
  { id: 'employees', name: 'Employees', icon: "/Icons/person-add.svg" },
  { id: 'reports', name: 'Reports', icon: "/Icons/archive.svg" },
  { id: 'analytics', name: 'Analytics', icon: "/Icons/clipboard-data.svg" },
];

export default function ProjectDashboard() {
  const params = useParams();
  const { user, logout } = useAuth();
  const [activeTab, setActiveTab] = useState('profile');
  const [projectInfo, setProjectInfo] = useState<any>(null);
  const projectSlug = params.slug as string;

  useEffect(() => {
    // Load project information
    // This would typically fetch from API
    setProjectInfo({
      name: projectSlug.replace('-', ' ').toUpperCase(),
      slug: projectSlug,
      description: 'Project management dashboard'
    });
  }, [projectSlug]);

  const renderTabContent = () => {
    switch (activeTab) {
      case 'profile':
        return <ProjectProfileTab projectSlug={projectSlug} />;
      case 'analytics':
        return <ProjectAnalyticsTab projectSlug={projectSlug} />;
      case 'reports':
        return <ProjectReportsTab projectSlug={projectSlug} />;
      case 'participants':
        return <ProjectParticipantsTab projectSlug={projectSlug} />;
      case 'employees':
        return <ProjectEmployeesTab projectSlug={projectSlug} />;
      case 'surveys':
        return <ProjectSurveysTab projectSlug={projectSlug} />;
      case 'questions':
        return <ProjectQuestionsTab projectSlug={projectSlug} />;
      default:
        return <ProjectProfileTab projectSlug={projectSlug} />;
    }
  };

  return (
    <ProtectedRoute allowedRoles={['SuperAdmin', 'TenantAdmin']}>
      <div className="min-h-screen bg-gray-50">
        {/* Navigation */}
        <nav className="bg-white shadow-sm border-b border-blue-700">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex justify-between h-16">
              <div className="flex items-center">
                <Image
                  src="/logos/logo-small.png"
                  alt="Nomad Surveys"
                  width={150}
                  height={60}
                  className="h-12 w-auto"
                />
                <div className="ml-6">
                  <h1 className="text-xl font-semibold text-black">
                    {projectInfo?.name || 'Project Dashboard'}
                  </h1>
                  <h1 className="text-sm text-black opacity-90">
                    Project: {projectSlug}
                  </h1>
                </div>
              </div>

              <div className="flex items-center space-x-4">
                <div className="text-sm text-black">
                  <span className="font-medium">{user?.fullName}</span>
                </div>
                <button
                  onClick={() => window.close()}
                  className="bg-gray-500 hover:bg-gray-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors mr-2"
                >
                  Close
                </button>
                <button
                  onClick={logout}
                  className="bg-blue-500 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors"
                >
                  Logout
                </button>
              </div>
            </div>
          </div>
        </nav>

        {/* Tab Navigation */}
        <div className="bg-white border-b border-gray-200">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <nav className="flex space-x-8" aria-label="Tabs">
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`${
                    activeTab === tab.id
                      ? 'border-blue-500 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm flex items-center space-x-2`}
                >
                  <Image
                    src={tab.icon}
                    alt={tab.name}
                    width={20}
                    height={20}
                    className="w-5 h-5"
                  />
                  <span>{tab.name}</span>
                </button>
              ))}
            </nav>
          </div>
        </div>

        {/* Main Content */}
        <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
          <div className="px-4 py-6 sm:px-0">
            {renderTabContent()}
          </div>
        </main>
      </div>
    </ProtectedRoute>
  );
}
