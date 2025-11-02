'use client';

import React, { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';
import LoginForm from '@/components/LoginForm';

export default function LoginPage() {
  const { isAuthenticated, user } = useAuth();
  const router = useRouter();

  useEffect(() => {
    // Add a small delay to ensure state is properly set
    const timer = setTimeout(() => {
      if (isAuthenticated && user) {
        // Handle both camelCase and PascalCase roles
        const roles = user.roles || user.Roles || [];
        if (roles.length > 0) {
          // Redirect to appropriate dashboard based on role
          const role = roles[0];
          // Get tenant slug for tenant-specific routes
          const tenantSlug = user.Tenant?.Slug || user.Tenant?.slug || user.tenant?.Slug || user.tenant?.slug;

          console.log('Login page redirect - User role:', role, 'Tenant slug:', tenantSlug);
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
                console.error('Participant user has no tenant slug');
              }
              break;
            default:
              console.warn('Unknown role:', role);
              break;
          }
        } else {
          console.warn('User has no roles:', user);
        }
      }
    }, 100); // Small delay to ensure state is set

    return () => clearTimeout(timer);
  }, [isAuthenticated, user, router]);

  if (isAuthenticated) {
    return null; // Will redirect
  }

  return <LoginForm />;
}
