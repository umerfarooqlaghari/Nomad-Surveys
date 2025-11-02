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
        // Handle both camelCase and PascalCase roles
        const roles = user.roles || user.Roles || [];
        if (roles.length > 0) {
          // Redirect to appropriate dashboard based on role
          const role = roles[0];
          // Get tenant slug for tenant-specific routes
          const tenantSlug = user.Tenant?.Slug || user.Tenant?.slug || user.tenant?.Slug || user.tenant?.slug;

          switch (role) {
            case 'SuperAdmin':
              router.push('/superadmin/dashboard');
              break;
            case 'TenantAdmin':
              router.push('/admin/dashboard');
              break;
            case 'Participant':
              if (tenantSlug) {
                router.push(`/${tenantSlug}/participant/dashboard`);
              } else {
                console.warn('Participant user has no tenant, redirecting to login');
                router.push('/login');
              }
              break;
            default:
              console.warn('Unknown role, redirecting to login:', role);
              router.push('/login');
          }
        } else {
          console.warn('User has no roles, redirecting to login:', user);
          router.push('/login');
        }
      } else {
        console.log('Not authenticated, redirecting to login. isAuthenticated:', isAuthenticated, 'user:', user);
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
