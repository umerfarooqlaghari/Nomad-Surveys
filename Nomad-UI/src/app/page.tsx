'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';

export default function Home() {
  const router = useRouter();
  const { isAuthenticated, user, isLoading } = useAuth();

  useEffect(() => {
    if (!isLoading) {
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
            router.push('/login');
        }
      } else {
        router.push('/login');
      }
    }
  }, [isAuthenticated, user, isLoading, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-indigo-600"></div>
      </div>
    );
  }

  return null;
}
