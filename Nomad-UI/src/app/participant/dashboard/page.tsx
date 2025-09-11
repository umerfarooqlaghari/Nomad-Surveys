'use client';

import React from 'react';
import ProtectedRoute from '@/components/ProtectedRoute';
import DashboardLayout from '@/components/DashboardLayout';

export default function ParticipantDashboard() {
  return (
    <ProtectedRoute allowedRoles={['Participant']}>
      <DashboardLayout title="Participant Dashboard">
        <div className="bg-white shadow rounded-lg p-6">
          <h2 className="text-2xl font-bold text-gray-900 mb-4">
            Welcome to Participant Dashboard
          </h2>
          <p className="text-gray-600">
            This is the Participant dashboard where you can view and complete assigned surveys.
          </p>
          
          <div className="mt-8 grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="bg-blue-50 p-6 rounded-lg">
              <h3 className="text-lg font-semibold text-blue-900 mb-2">Available Surveys</h3>
              <p className="text-blue-700">View and complete surveys assigned to you.</p>
            </div>
            
            <div className="bg-green-50 p-6 rounded-lg">
              <h3 className="text-lg font-semibold text-green-900 mb-2">Completed Surveys</h3>
              <p className="text-green-700">Review your completed survey submissions.</p>
            </div>
          </div>
        </div>
      </DashboardLayout>
    </ProtectedRoute>
  );
}
