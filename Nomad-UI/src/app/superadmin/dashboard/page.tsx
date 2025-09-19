/* eslint-disable @next/next/no-img-element */
/* eslint-disable react/jsx-no-undef */
'use client';

import React, { useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import ProtectedRoute from '@/components/ProtectedRoute';
import DashboardLayout from '@/components/DashboardLayout';
import OverviewTab from '@/components/superadmin/OverviewTab';
import ProjectsTab from '@/components/superadmin/ProjectsTab';
import SurveyBuilderTab from '@/components/superadmin/SurveyBuilderTab';
import ReportsTab from '@/components/superadmin/ReportsTab';

const tabs = [
  { id: 'overview', name: 'Overview', icon: "/Icons/yelp.svg" },
  { id: 'projects', name: 'Projects', icon: "/Icons/buildings.svg" },
  { id: 'survey-builder', name: 'Survey Builder', icon: "/Icons/wrench-adjustable-circle.svg" },
  { id: 'reports', name: 'Reports', icon: "/Icons/archive.svg" },
];

export default function SuperAdminDashboard() {
  const { user, isAuthenticated, isLoading } = useAuth();
  const [activeTab, setActiveTab] = useState('overview');

  console.log('SuperAdmin Dashboard - User:', user, 'isAuthenticated:', isAuthenticated, 'isLoading:', isLoading);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-lg">Loading...</div>
      </div>
    );
  }

  const renderTabContent = () => {
    try {
     switch (activeTab) {
      case 'overview':
        return <OverviewTab />;
      case 'projects':
        return <ProjectsTab />;
      case 'survey-builder':
        return <SurveyBuilderTab />;
      case 'reports':
        return <ReportsTab />;
      default:
        return <OverviewTab />;
  }
    } catch (error) {
      console.error('Error rendering tab content:', error);
      return <div className="p-4 bg-red-100 text-red-700 rounded-lg">Error loading content</div>;
    }
  };

  return (
    <ProtectedRoute allowedRoles={['SuperAdmin']}>
      <DashboardLayout title="SuperAdmin Dashboard">
        <div className="space-y-6">
          <div>
            <h1 className="text-2xl font-semibold text-gray-900">SuperAdmin Dashboard</h1>
            <p className="mt-1 text-sm text-gray-600">
              Welcome back, {user?.firstName || user?.FirstName} {user?.lastName || user?.LastName}
            </p>
          </div>

          {/* Tab Navigation */}
          <div className="border-b border-gray-200">
            <nav className="-mb-px flex space-x-8" aria-label="Tabs">
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`${
                    activeTab === tab.id
                      ? 'border-blue-500 text-blue-600'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  } whitespace-nowrap py-2 px-1 border-b-2 font-medium text-sm flex items-center space-x-2`}
                >
                  <img
                    src={tab.icon}
                    alt={`${tab.name} icon`}
                    width={20}
                    height={20}
                    className="w-5 h-5"
                  />                
                    <span>{tab.name}</span>
                </button>
              ))}
            </nav>
          </div>

          {/* Tab Content */}
          <div className="mt-6">
            {renderTabContent()}
          </div>
        </div>
      </DashboardLayout>
    </ProtectedRoute>
  );
}
