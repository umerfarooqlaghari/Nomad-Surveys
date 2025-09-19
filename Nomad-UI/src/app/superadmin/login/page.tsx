'use client';

import React, { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';
import LoginForm from '@/components/LoginForm';

export default function SuperAdminLoginPage() {
  const { isAuthenticated, user } = useAuth();
  const router = useRouter();

  useEffect(() => {
    // Add a small delay to ensure state is properly set
    const timer = setTimeout(() => {
      if (isAuthenticated && user) {
        // Handle both camelCase and PascalCase roles
        const roles = user.roles || user.Roles || [];
        if (roles.length > 0) {
          // Redirect SuperAdmin to their dashboard, others to appropriate dashboard
          const role = roles[0];
          console.log('SuperAdmin login page redirect - User role:', role, 'User:', user);
          switch (role) {
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
              console.warn('Unknown role, redirecting to login:', role);
              router.push('/login');
          }
        } else {
          console.warn('User has no roles:', user);
          router.push('/login');
        }
      }
    }, 100); // Small delay to ensure state is set

    return () => clearTimeout(timer);
  }, [isAuthenticated, user, router]);

  if (isAuthenticated) {
    return null; // Will redirect
  }

  return <LoginForm isSuperAdmin={true} />;
}
