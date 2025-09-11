'use client';

import React, { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';
import LoginForm from '@/components/LoginForm';

export default function SuperAdminLoginPage() {
  const { isAuthenticated, user } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isAuthenticated && user) {
      // Redirect SuperAdmin to their dashboard, others to appropriate dashboard
      const role = user.roles[0];
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
          router.push('/login');
      }
    }
  }, [isAuthenticated, user, router]);

  if (isAuthenticated) {
    return null; // Will redirect
  }

  return <LoginForm isSuperAdmin={true} />;
}
