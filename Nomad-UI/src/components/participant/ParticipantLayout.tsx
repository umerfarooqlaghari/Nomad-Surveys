'use client';

import React, { useState, useMemo } from 'react';
import { useRouter, usePathname, useParams } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';
import {
  HomeIcon,
  ClipboardDocumentListIcon,
  DocumentCheckIcon,
  QuestionMarkCircleIcon,
  UserCircleIcon,
  ArrowRightOnRectangleIcon,
  Bars3Icon,
  XMarkIcon,
} from '@heroicons/react/24/outline';

interface ParticipantLayoutProps {
  children: React.ReactNode;
}

export default function ParticipantLayout({ children }: ParticipantLayoutProps) {
  const router = useRouter();
  const pathname = usePathname();
  const params = useParams();
  const { user, tenant, logout } = useAuth();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  // Get tenant slug from params or from auth context
  const tenantSlug = (params?.tenantSlug as string) || tenant?.Slug || tenant?.slug;

  // Build navigation with tenant slug
  const navigation = useMemo(() => {
    if (!tenantSlug) return [];

    return [
      { name: 'Dashboard', href: `/${tenantSlug}/participant/dashboard`, icon: HomeIcon },
      { name: 'Assigned Evaluations', href: `/${tenantSlug}/participant/evaluations`, icon: ClipboardDocumentListIcon },
      { name: 'My Submissions', href: `/${tenantSlug}/participant/submissions`, icon: DocumentCheckIcon },
      { name: 'Help & Support', href: `/${tenantSlug}/participant/help`, icon: QuestionMarkCircleIcon },
      { name: 'Profile', href: `/${tenantSlug}/participant/profile`, icon: UserCircleIcon },
    ];
  }, [tenantSlug]);

  const handleLogout = () => {
    logout();
  };

  const isActive = (href: string) => pathname === href;

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Mobile sidebar backdrop */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 bg-black bg-opacity-50 z-40 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Sidebar */}
      <div
        className={`fixed inset-y-0 left-0 z-50 w-64 bg-white border-r border-gray-200 transform transition-transform duration-300 ease-in-out lg:translate-x-0 ${
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        {/* Logo */}
        <div className="flex items-center justify-between h-16 px-6 border-b border-gray-200">
          <h1 className="text-xl font-bold text-black">Nomad Surveys</h1>
          <button
            onClick={() => setSidebarOpen(false)}
            className="lg:hidden text-gray-500 hover:text-gray-700"
          >
            <XMarkIcon className="h-6 w-6" />
          </button>
        </div>

        {/* User info */}
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex items-center space-x-3">
            <div className="flex-shrink-0">
              <div className="h-10 w-10 rounded-full bg-blue-600 flex items-center justify-center">
                <span className="text-white font-semibold text-sm">
                  {user?.FirstName?.[0]}{user?.LastName?.[0]}
                </span>
              </div>
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-black truncate">
                {user?.FirstName} {user?.LastName}
              </p>
              <p className="text-xs text-gray-600 truncate">{user?.Email}</p>
            </div>
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 px-4 py-4 space-y-1">
          {navigation.map((item) => {
            const Icon = item.icon;
            const active = isActive(item.href);
            return (
              <button
                key={item.name}
                onClick={() => {
                  router.push(item.href);
                  setSidebarOpen(false);
                }}
                className={`w-full flex items-center px-4 py-3 text-sm font-medium rounded-lg transition-colors ${
                  active
                    ? 'bg-blue-600 text-white'
                    : 'text-black hover:bg-gray-100'
                }`}
              >
                <Icon className="h-5 w-5 mr-3" />
                {item.name}
              </button>
            );
          })}
        </nav>

        {/* Logout button */}
        <div className="px-4 py-4 border-t border-gray-200">
          <button
            onClick={handleLogout}
            className="w-full flex items-center px-4 py-3 text-sm font-medium text-black hover:bg-gray-100 rounded-lg transition-colors"
          >
            <ArrowRightOnRectangleIcon className="h-5 w-5 mr-3" />
            Logout
          </button>
        </div>
      </div>

      {/* Main content */}
      <div className="lg:pl-64">
        {/* Top bar */}
        <div className="sticky top-0 z-30 bg-white border-b border-gray-200 h-16 flex items-center px-4 lg:px-8">
          <button
            onClick={() => setSidebarOpen(true)}
            className="lg:hidden text-gray-500 hover:text-gray-700 mr-4"
          >
            <Bars3Icon className="h-6 w-6" />
          </button>
          <h2 className="text-lg font-semibold text-black">
            {navigation.find((item) => isActive(item.href))?.name || 'Participant Portal'}
          </h2>
        </div>

        {/* Page content */}
        <main className="p-4 lg:p-8">
          {children}
        </main>
      </div>
    </div>
  );
}

