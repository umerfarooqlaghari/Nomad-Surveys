/* eslint-disable react/no-unescaped-entities */
'use client';

import React from 'react';
import ProtectedRoute from '@/components/ProtectedRoute';
import DashboardLayout from '@/components/DashboardLayout';

export default function TenantAdminDashboard() {
  return (
    <ProtectedRoute allowedRoles={['TenantAdmin']}>
      <DashboardLayout title="Tenant Admin Dashboard">
        <div className="bg-white shadow rounded-lg p-6">
          <h2 className="text-2xl font-bold text-gray-900 mb-4">
            Welcome to Tenant Admin Dashboard
          </h2>
          <p className="text-gray-600">
            This is the Tenant Admin dashboard where you can manage your organization's surveys, users, and settings.
          </p>
          
          <div className="mt-8 grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="bg-blue-50 p-6 rounded-lg">
              <h3 className="text-lg font-semibold text-blue-900 mb-2">Survey Management</h3>
              <p className="text-blue-700">Create and manage surveys for your organization.</p>
            </div>
            
            <div className="bg-green-50 p-6 rounded-lg">
              <h3 className="text-lg font-semibold text-green-900 mb-2">User Management</h3>
              <p className="text-green-700">Manage users within your organization.</p>
            </div>
            
            <div className="bg-purple-50 p-6 rounded-lg">
              <h3 className="text-lg font-semibold text-purple-900 mb-2">Reports & Analytics</h3>
              <p className="text-purple-700">View survey results and analytics.</p>
            </div>
          </div>
        </div>
      </DashboardLayout>
    </ProtectedRoute>
  );
}
