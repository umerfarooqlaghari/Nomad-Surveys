'use client';

import React, { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';
import LoginForm from '@/components/LoginForm';

export default function LoginPage() {
  const { isAuthenticated, user } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isAuthenticated && user) {
      // Redirect to appropriate dashboard based on role
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
          break;
      }
    }
  }, [isAuthenticated, user, router]);

  if (isAuthenticated) {
    return null; // Will redirect
  }

  return <LoginForm />;
}
