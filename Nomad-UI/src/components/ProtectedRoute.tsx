'use client';

import React, { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';
import { UserRole } from '@/types/auth';

interface ProtectedRouteProps {
  children: React.ReactNode;
  allowedRoles?: UserRole[];
  redirectTo?: string;
}

export default function ProtectedRoute({ 
  children, 
  allowedRoles = [], 
  redirectTo = '/login' 
}: ProtectedRouteProps) {
  const { user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    // console.log('ProtectedRoute - isLoading:', isLoading, 'isAuthenticated:', isAuthenticated, 'user:', user, 'allowedRoles:', allowedRoles);

    if (!isLoading) {
      if (!isAuthenticated) {
        // console.log('Not authenticated, redirecting to:', redirectTo);
        router.push(redirectTo);
        return;
      }

      if (allowedRoles.length > 0 && user) {
        // Handle both camelCase and PascalCase roles
        const roles = user.roles || user.Roles || [];
        if (roles.length > 0) {
          const userRole = roles[0] as UserRole;
          if (!allowedRoles.includes(userRole)) {
            // Redirect to appropriate dashboard based on user role
            switch (userRole) {
              case 'SuperAdmin':
                router.push('/superadmin/dashboard');
                break;
              case 'TenantAdmin':
                router.push('/admin/dashboard');
                break;
              case 'Participant':
                router.push('/participant/dashboard');
                break;
              default:
                router.push('/login');
            }
          }
        } else {
          console.warn('User has no roles in ProtectedRoute:', user);
          router.push('/login');
        }
      }
    }
  }, [isAuthenticated, isLoading, user, allowedRoles, router, redirectTo]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-indigo-600"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  if (allowedRoles.length > 0 && user) {
    // Handle both camelCase and PascalCase roles
    const roles = user.roles || user.Roles || [];
    if (roles.length === 0) {
      console.warn('User has no roles:', user);
      return null;
    }

    const userRole = roles[0] as UserRole;
    if (!allowedRoles.includes(userRole)) {
      console.warn('User role not allowed:', userRole, 'Allowed:', allowedRoles);
      return null;
    }
  }

  return <>{children}</>;
}
