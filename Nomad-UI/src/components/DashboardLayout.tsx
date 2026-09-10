'use client';

import React, { useState } from 'react';
import Image from 'next/image';
import Link from 'next/link';
import { useAuth } from '@/contexts/AuthContext';
import LogoutConfirmationModal from '@/components/modals/LogoutConfirmationModal';

interface DashboardLayoutProps {
  children: React.ReactNode;
  title: string;
}

export default function DashboardLayout({ children }: DashboardLayoutProps) {
  const { user, tenant, logout } = useAuth();
  const [showLogoutModal, setShowLogoutModal] = useState(false);

  const handleLogoutClick = () => {
    setShowLogoutModal(true);
  };

  const handleLogoutConfirm = () => {
    setShowLogoutModal(false);
    logout();
  };

  const handleLogoutCancel = () => {
    setShowLogoutModal(false);
  };

  return (
    <div className="min-h-screen bg-gray-50 text-gray-900">
      {/* Navigation */}
      <nav className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <Image
                src="/logos/logo-small.png"
                alt="Alpha Surveys"
                width={150}
                height={60}
                className="h-10 w-auto"
              />
              <div className="ml-5">
                <h1 className="text-lg font-bold text-gray-900 leading-tight">Alpha</h1>
                <p className="text-xs text-gray-500">By Alpha Development</p>
              </div>
            </div>

            <div className="flex items-center space-x-4">
              <div className="text-sm text-gray-700">
                <span className="font-medium text-gray-900">{user?.fullName || user?.FullName || 'User'}</span>
                {tenant && (
                  <span className="ml-2 text-gray-500">({tenant.name})</span>
                )}
              </div>
              {tenant && (
                <Link
                  href={`/${tenant.slug}/admin/report-template`}
                  className="bg-blue-600 hover:bg-blue-700 text-white px-3 py-2 rounded-md text-sm font-medium transition-colors"
                >
                  Edit Template
                </Link>
              )}
              <button
                onClick={handleLogoutClick}
                className="bg-gray-100 hover:bg-gray-200 text-gray-700 border border-gray-300 px-4 py-2 rounded-md text-sm font-medium transition-colors cursor-pointer"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          {children}
        </div>
      </main>

      {/* Logout Confirmation Modal */}
      <LogoutConfirmationModal
        isOpen={showLogoutModal}
        onConfirm={handleLogoutConfirm}
        onCancel={handleLogoutCancel}
      />
    </div>
  );
}
