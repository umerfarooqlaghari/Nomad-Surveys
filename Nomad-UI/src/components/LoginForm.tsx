/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState } from 'react';
import Image from 'next/image';
import { useAuth } from '@/contexts/AuthContext';
import toast, { Toaster } from 'react-hot-toast';
import styles from './LoginForm.module.css';
import ForgotPasswordModal from './modals/ForgotPasswordModal';
import VerifyOtpModal from './modals/VerifyOtpModal';

interface LoginFormProps {
  isSuperAdmin?: boolean;
}

export default function LoginForm({ isSuperAdmin = false }: LoginFormProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [tenantSlug, setTenantSlug] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showForgotPasswordModal, setShowForgotPasswordModal] = useState(false);
  const [showVerifyOtpModal, setShowVerifyOtpModal] = useState(false);
  const [resetEmail, setResetEmail] = useState('');
  const [resetTenantSlug, setResetTenantSlug] = useState('');

  const { login, superAdminLogin } = useAuth();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      if (isSuperAdmin) {
        await superAdminLogin({
          email: email.trim(),
          password,
          rememberMe,
        });
        toast.success('Login successful! Welcome back.');
      } else {
        await login({
          email: email.trim(),
          password,
          tenantSlug: tenantSlug.trim(),
          rememberMe,
        });
        toast.success('Login successful! Welcome back.');
      }
    } catch (err: any) {
      const errorMessage = err.message || 'Login failed. Please try again.';
      setError(errorMessage);
      toast.error(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const handleForgotPasswordClick = (e: React.MouseEvent) => {
    e.preventDefault();
    setShowForgotPasswordModal(true);
  };

  const handleOtpSent = (emailAddress: string, slug: string) => {
    setResetEmail(emailAddress);
    setResetTenantSlug(slug);
    setShowVerifyOtpModal(true);
  };

  const handlePasswordResetSuccess = () => {
    toast.success('Password reset successfully! You can now login with your new password.');
    setResetEmail('');
  };

  return (
    <>
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 4000,
          style: {
            background: '#18181b',
            color: '#fff',
            border: '1px solid rgba(255, 255, 255, 0.1)',
            backdropFilter: 'blur(10px)',
          },
          success: {
            duration: 3000,
            style: {
              background: 'rgba(16, 185, 129, 0.9)',
            },
          },
          error: {
            duration: 5000,
            style: {
              background: 'rgba(239, 68, 68, 0.9)',
            },
          },
        }}
      />

      <div className={styles.container}>
        {/* Background Image & Overlay */}
        <div className={styles.backgroundImageWrapper}>
          <Image
            src="/Background/LoginScreenBackground.png"
            alt="Login Background"
            fill
            sizes="100vw"
            className={styles.backgroundImage}
            priority
          />
          <div className={styles.backgroundOverlay} />
        </div>

        {/* Main Split Layout */}
        <div className={styles.mainLayout}>
          {/* Left Side: Clean Typography Hero */}
          <div className={styles.leftSide}>
            <div className={styles.heroContent}>
              <h1 className={styles.heroTitle}>
                Welcome to<br />Alpha Surveys
              </h1>
              <p className={styles.heroSubtitle}>
                Access your surveys, manage responses, and unlock deep analytical insights in one unified workspace.
              </p>
            </div>
          </div>

          {/* Right Side: Frosted Glass Login Panel */}
          <div className={styles.rightSide}>
            <div className={styles.formCard}>
              <div className={styles.header}>
                <h2 className={styles.title}>Sign In</h2>
                {isSuperAdmin && (
                  <p className={styles.subtitle}>SuperAdmin Management Portal</p>
                )}
              </div>

              <form className={styles.form} onSubmit={handleSubmit}>
                <div className={styles.fieldsWrapper}>
                  {/* Email Field */}
                  <div className={styles.fieldGroup}>
                    <label htmlFor="email" className={styles.label}>
                      Email
                    </label>
                    <div className={styles.inputWrapper}>
                      <input
                        id="email"
                        name="email"
                        type="email"
                        autoComplete="email"
                        required
                        className={styles.input}
                        placeholder="Enter your email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                      />
                      <span className={styles.icon}>
                        {/* Mail icon */}
                        <svg fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={1.5}
                            d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                          />
                        </svg>
                      </span>
                    </div>
                  </div>

                  {/* Company Code Field (Tenant Slug) */}
                  {!isSuperAdmin && (
                    <div className={styles.fieldGroup}>
                      <label htmlFor="tenantSlug" className={styles.label}>
                        Company Code
                      </label>
                      <div className={styles.inputWrapper}>
                        <input
                          id="tenantSlug"
                          name="tenantSlug"
                          type="text"
                          required
                          className={styles.input}
                          placeholder="Enter your company code"
                          value={tenantSlug}
                          onChange={(e) => setTenantSlug(e.target.value)}
                        />
                        <span className={styles.icon}>
                          {/* Organization / Building Icon */}
                          <svg fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={1.5}
                              d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
                            />
                          </svg>
                        </span>
                      </div>
                    </div>
                  )}

                  {/* Password Field */}
                  <div className={styles.fieldGroup}>
                    <label htmlFor="password" className={styles.label}>
                      Password
                    </label>
                    <div className={styles.inputWrapper}>
                      <input
                        id="password"
                        name="password"
                        type={showPassword ? 'text' : 'password'}
                        autoComplete="current-password"
                        required
                        className={styles.input}
                        placeholder="Enter your password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                      />
                      <button
                        type="button"
                        className={styles.iconButton}
                        onClick={() => setShowPassword(!showPassword)}
                        aria-label={showPassword ? 'Hide password' : 'Show password'}
                        title={showPassword ? 'Hide password' : 'Show password'}
                      >
                        {showPassword ? (
                          /* Eye Off */
                          <svg fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={1.5}
                              d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21"
                            />
                          </svg>
                        ) : (
                          /* Lock icon */
                          <svg fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={1.5}
                              d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                            />
                          </svg>
                        )}
                      </button>
                    </div>
                  </div>
                </div>

                {/* Checkbox Row */}
                <div className={styles.checkboxRow}>
                  <label className={styles.checkboxContainer} htmlFor="remember-me">
                    <input
                      id="remember-me"
                      name="remember-me"
                      type="checkbox"
                      className={styles.checkbox}
                      checked={rememberMe}
                      onChange={(e) => setRememberMe(e.target.checked)}
                    />
                    <span className={styles.checkboxLabel}>Remember me</span>
                  </label>
                </div>

                {/* Error Banner */}
                {error && (
                  <div className={styles.errorMessage}>
                    {error}
                  </div>
                )}

                {/* Actions Row */}
                <div className={styles.actionsRow}>
                  <button
                    type="submit"
                    disabled={isLoading}
                    className={styles.submitButton}
                  >
                    <span>{isLoading ? 'Signing in...' : 'Sign In'}</span>
                    <span className={styles.arrowIcon}>&gt;</span>
                  </button>

                  <a
                    href="#"
                    className={styles.forgotLink}
                    onClick={handleForgotPasswordClick}
                  >
                    Reset Password?
                  </a>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>

      {/* Password Reset Modals */}
      <ForgotPasswordModal
        isOpen={showForgotPasswordModal}
        onClose={() => setShowForgotPasswordModal(false)}
        onOtpSent={handleOtpSent}
        initialTenantSlug={isSuperAdmin ? '' : tenantSlug.trim()}
      />

      <VerifyOtpModal
        isOpen={showVerifyOtpModal}
        onClose={() => setShowVerifyOtpModal(false)}
        onSuccess={handlePasswordResetSuccess}
        email={resetEmail}
        tenantSlug={isSuperAdmin ? '' : resetTenantSlug}
      />
    </>
  );
}
