'use client';

import React, { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { AuthContextType, User, Tenant, LoginRequest, SuperAdminLoginRequest } from '@/types/auth';
import { AuthService } from '@/lib/auth';

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [tenant, setTenant] = useState<Tenant | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  // Auto-logout timer
  const [logoutTimer, setLogoutTimer] = useState<NodeJS.Timeout | null>(null);
  const [activityTimer, setActivityTimer] = useState<NodeJS.Timeout | null>(null);

  const logout = useCallback(() => {
    AuthService.clearAuth();
    setUser(null);
    setTenant(null);
    setToken(null);

    // Clear timers
    if (logoutTimer) clearTimeout(logoutTimer);
    if (activityTimer) clearTimeout(activityTimer);

    router.push('/login');
  }, [router]); // Remove timer dependencies

  const resetActivityTimer = useCallback(() => {
    if (activityTimer) clearTimeout(activityTimer);

    // Set 30-minute inactivity timer
    const newTimer = setTimeout(() => {
      logout();
    }, 30 * 60 * 1000); // 30 minutes

    setActivityTimer(newTimer);
  }, [logout]); // Remove activityTimer dependency

  const setupAutoLogout = useCallback((expiresAt: string) => {
    const expiryTime = new Date(expiresAt).getTime();
    const currentTime = new Date().getTime();
    const timeUntilExpiry = expiryTime - currentTime;

    if (timeUntilExpiry > 0) {
      const timer = setTimeout(() => {
        logout();
      }, timeUntilExpiry);
      setLogoutTimer(timer);
    } else {
      logout();
    }
  }, [logout]);

  const login = async (credentials: LoginRequest) => {
    try {
      setIsLoading(true);
      const response = await AuthService.login(credentials);

      AuthService.setToken(response.accessToken);
      AuthService.setUser(response.user);
      AuthService.setTokenExpiry(response.expiresAt);

      if (response.tenant) {
        AuthService.setTenant(response.tenant);
        setTenant(response.tenant);
      }

      setUser(response.user);
      setToken(response.accessToken);

      // Setup auto-logout timer directly
      const expiryTime = new Date(response.expiresAt).getTime();
      const currentTime = new Date().getTime();
      const timeUntilExpiry = expiryTime - currentTime;

      if (timeUntilExpiry > 0) {
        const timer = setTimeout(() => {
          logout();
        }, timeUntilExpiry);
        setLogoutTimer(timer);
      }

      // Set activity timer directly
      const activityTimer = setTimeout(() => {
        logout();
      }, 30 * 60 * 1000);
      setActivityTimer(activityTimer);

      // Redirect based on role
      const dashboardRoute = AuthService.getDashboardRoute();
      router.push(dashboardRoute);
    } catch (error) {
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const superAdminLogin = async (credentials: SuperAdminLoginRequest) => {
    try {
      setIsLoading(true);
      const response = await AuthService.superAdminLogin(credentials);

      AuthService.setToken(response.accessToken);
      AuthService.setUser(response.user);
      AuthService.setTokenExpiry(response.expiresAt);

      setUser(response.user);
      setToken(response.accessToken);

      // Setup auto-logout timer directly
      const expiryTime = new Date(response.expiresAt).getTime();
      const currentTime = new Date().getTime();
      const timeUntilExpiry = expiryTime - currentTime;

      if (timeUntilExpiry > 0) {
        const timer = setTimeout(() => {
          logout();
        }, timeUntilExpiry);
        setLogoutTimer(timer);
      }

      // Set activity timer directly
      const activityTimer = setTimeout(() => {
        logout();
      }, 30 * 60 * 1000);
      setActivityTimer(activityTimer);

      router.push('/superadmin/dashboard');
    } catch (error) {
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  // Initialize auth state from localStorage
  useEffect(() => {
    const initializeAuth = () => {
      const storedToken = AuthService.getToken();
      const storedUser = AuthService.getUser();
      const storedTenant = AuthService.getTenant();

      if (storedToken && storedUser && !AuthService.isTokenExpired()) {
        setToken(storedToken);
        setUser(storedUser);
        if (storedTenant) setTenant(storedTenant);

        // Setup auto-logout for existing session
        const expiry = localStorage.getItem('tokenExpiry');
        if (expiry) {
          const expiryTime = new Date(parseInt(expiry)).getTime();
          const currentTime = new Date().getTime();
          const timeUntilExpiry = expiryTime - currentTime;

          if (timeUntilExpiry > 0) {
            const timer = setTimeout(() => {
              logout();
            }, timeUntilExpiry);
            setLogoutTimer(timer);
          } else {
            logout();
            return;
          }
        }

        // Set 30-minute inactivity timer
        const activityTimer = setTimeout(() => {
          logout();
        }, 30 * 60 * 1000);
        setActivityTimer(activityTimer);
      } else {
        AuthService.clearAuth();
      }

      setIsLoading(false);
    };

    initializeAuth();
  }, []); // Empty dependency array

  // Activity tracking
  useEffect(() => {
    if (!user) return;

    const handleActivity = () => {
      if (activityTimer) clearTimeout(activityTimer);

      // Set 30-minute inactivity timer
      const newTimer = setTimeout(() => {
        logout();
      }, 30 * 60 * 1000);

      setActivityTimer(newTimer);
    };

    const events = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart', 'click'];
    events.forEach(event => {
      document.addEventListener(event, handleActivity, true);
    });

    return () => {
      events.forEach(event => {
        document.removeEventListener(event, handleActivity, true);
      });
    };
  }, [user]); // Remove resetActivityTimer dependency

  // Handle tab close/refresh
  useEffect(() => {
    const handleBeforeUnload = () => {
      // Optional: You can choose to logout on tab close
      // logout();
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, []);

  const value: AuthContextType = {
    user,
    tenant,
    token,
    isAuthenticated: !!user && !!token,
    login,
    superAdminLogin,
    logout,
    isLoading,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
