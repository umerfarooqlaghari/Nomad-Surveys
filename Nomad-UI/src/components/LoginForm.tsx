/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState } from 'react';
import Image from 'next/image';
import { useAuth } from '@/contexts/AuthContext';
import toast, { Toaster } from 'react-hot-toast';
import styles from './LoginForm.module.css';
import Link from 'next/link';
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
            background: '#363636',
            color: '#fff',
          },
          success: {
            duration: 3000,
            style: {
              background: '#10B981',
            },
          },
          error: {
            duration: 5000,
            style: {
              background: '#EF4444',
            },
          },
        }}
      />
      <div className={styles.container}>
        {/* Left Side - Background Image with Logo */}
        <div className={styles.leftSide}>
          {/* Background Image */}
          <div className={styles.backgroundImage}>
            <Image
              src="/Background/LoginScreenBackground.jpg"
              alt="Login Background"
              fill
              style={{ objectFit: 'cover' }}
              priority
            />
            <div className={styles.backgroundOverlay}></div>
          </div>

          {/* Content */}
          <div className={styles.content}>
            {/* Large Logo */}
            <div className={styles.logoContainer}>
              <div className={styles.logoText}>
                NOM<span className={styles.logoAccent}>A</span>D
              </div>
            </div>

            {/* Tagline */}
            <h1 className={styles.tagline}>
              Insight-driven decisions for people and organizations
            </h1>

            {/* Description */}
            <p className={styles.description}>
              Nomad is a people insights platform that helps organizations gather structured, confidential input to support leadership development, engagement, and talent decisions. Built to deliver clarity, accuracy, and trust at every stage.
            </p>
          </div>
        </div>

        {/* Right Side - Login Form */}
        <div className={styles.rightSide}>
          <div className={styles.formContainer}>
            <div className={styles.header}>
              <h2>Sign In</h2>
              <p>
                {isSuperAdmin
                  ? 'Access the SuperAdmin dashboard'
                  : 'Access your account dashboard'
                }
              </p>
            </div>

            <form className={styles.form} onSubmit={handleSubmit}>
              <div className={styles.formFields}>
                <div className={styles.fieldGroup}>
                  <label htmlFor="email" className={styles.label}>
                    Email
                  </label>
                  <input
                    id="email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    required
                    className={styles.input}
                    placeholder="Email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                </div>

                <div className={styles.fieldGroup}>
                  <label htmlFor="password" className={styles.label}>
                    Password
                  </label>
                  <div className={styles.passwordContainer}>
                    <input
                      id="password"
                      name="password"
                      type={showPassword ? 'text' : 'password'}
                      autoComplete="current-password"
                      required
                      className={styles.passwordInput}
                      placeholder="Password"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                    />
                    <button
                      type="button"
                      className={styles.passwordToggle}
                      onClick={() => setShowPassword(!showPassword)}
                    >
                      {showPassword ? (
                        <svg fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21" />
                        </svg>
                      ) : (
                        <svg fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                        </svg>
                      )}
                    </button>
                  </div>
                </div>

                {!isSuperAdmin && (
                  <div className={styles.fieldGroup}>
                    <label htmlFor="tenantSlug" className={styles.label}>
                      Company Code
                    </label>
                    <input
                      id="tenantSlug"
                      name="tenantSlug"
                      type="text"
                      required
                      className={styles.input}
                      placeholder="Company Code"
                      value={tenantSlug}
                      onChange={(e) => setTenantSlug(e.target.value)}
                    />
                  </div>
                )}
              </div>

              <div className={styles.rememberForgot}>
                <div className={styles.rememberContainer}>
                  <input
                    id="remember-me"
                    name="remember-me"
                    type="checkbox"
                    className={styles.checkbox}
                    checked={rememberMe}
                    onChange={(e) => setRememberMe(e.target.checked)}
                  />
                  <label htmlFor="remember-me" className={styles.checkboxLabel}>
                    Remember me
                  </label>
                </div>
                <div>
                  <a
                    href="#"
                    className={styles.forgotLink}
                    onClick={handleForgotPasswordClick}
                  >
                    Reset Password?
                  </a>
                </div>
              </div>

              {error && (
                <div className={styles.errorMessage}>
                  <div className={styles.errorText}>{error}</div>
                </div>
              )}

              <div>
                <button
                  type="submit"
                  disabled={isLoading}
                  className={styles.submitButton}
                >
                  {isLoading ? 'Signing in...' : 'Sign In'}
                </button>
              </div>

          
            </form>
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
