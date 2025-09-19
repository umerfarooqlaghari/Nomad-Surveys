'use client';

import React from 'react';
import Image from 'next/image';
import { useAuth } from '@/contexts/AuthContext';

interface DashboardLayoutProps {
  children: React.ReactNode;
  title: string;
}

export default function DashboardLayout({ children }: DashboardLayoutProps) {
  const { user, tenant, logout } = useAuth();

  return (
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
                <h1 className="text-xl font-semibold text-black">Nomad</h1>
                <h1 className="text-sm text-black opacity-90">By Ascend Development</h1>
              </div>
            </div>

            <div className="flex items-center space-x-4">
              <div className="text-sm text-black">
                <span className="font-medium">{user?.fullName || user?.FullName || 'User'}</span>
                {tenant && (
                  <span className="ml-2 text-white opacity-75">({tenant.name})</span>
                )}
              </div>
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

      {/* Main Content */}
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          {children}
        </div>
      </main>
    </div>
  );
}
