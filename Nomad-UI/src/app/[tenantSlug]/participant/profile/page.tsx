'use client';

import React from 'react';
import ProtectedRoute from '@/components/ProtectedRoute';
import ParticipantLayout from '@/components/participant/ParticipantLayout';
import { UserCircleIcon } from '@heroicons/react/24/outline';

export default function ParticipantProfile() {
  return (
    <ProtectedRoute allowedRoles={['Participant']}>
      <ParticipantLayout>
        <div className="max-w-4xl mx-auto">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-2xl font-bold text-black">Profile</h1>
            <p className="text-sm text-black mt-1">
              Manage your profile settings
            </p>
          </div>

          {/* Empty State */}
          <div className="bg-white rounded-lg border border-gray-200 p-12 text-center">
            <UserCircleIcon className="h-16 w-16 text-gray-400 mx-auto mb-4" />
            <h2 className="text-lg font-semibold text-black mb-2">Profile Settings</h2>
            <p className="text-sm text-black">
              Profile management features will be available soon.
            </p>
          </div>
        </div>
      </ParticipantLayout>
    </ProtectedRoute>
  );
}

