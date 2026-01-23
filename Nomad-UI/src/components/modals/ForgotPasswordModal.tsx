/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState } from 'react';
import emailService from '@/services/emailService';
import toast from 'react-hot-toast';
import styles from './ForgotPasswordModal.module.css';

interface ForgotPasswordModalProps {
  isOpen: boolean;
  onClose: () => void;
  onOtpSent: (email: string, tenantSlug: string) => void;
  initialTenantSlug: string;
}

export default function ForgotPasswordModal({
  isOpen,
  onClose,
  onOtpSent,
  initialTenantSlug,
}: ForgotPasswordModalProps) {
  const [email, setEmail] = useState('');
  const [localTenantSlug, setLocalTenantSlug] = useState(initialTenantSlug);
  const [isLoading, setIsLoading] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email) {
      toast.error('Please enter your email address');
      return;
    }

    if (!localTenantSlug.trim()) {
      toast.error('Please enter your Company Code');
      return;
    }

    // Basic email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      toast.error('Please enter a valid email address');
      return;
    }

    setIsLoading(true);

    try {
      const trimmedEmail = email.trim();
      const trimmedSlug = localTenantSlug.trim();
      const result = await emailService.sendPasswordResetOtp(trimmedSlug, trimmedEmail);

      if (result.success) {
        toast.success(result.message);
        onOtpSent(trimmedEmail, trimmedSlug);
        onClose();
      } else {
        toast.error(result.error || 'Failed to send OTP');
      }
    } catch (error: any) {
      toast.error(error.message || 'An error occurred');
    } finally {
      setIsLoading(false);
    }
  };

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  return (
    <div className={styles.backdrop} onClick={handleBackdropClick}>
      <div className={styles.modal}>
        <div className={styles.header}>
          <h2 className={styles.title}>Forgot Password</h2>
          <button
            onClick={onClose}
            className={styles.closeButton}
            aria-label="Close"
          >
            <svg className={styles.closeIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <form onSubmit={handleSubmit} className={styles.form}>
          <p className={styles.description}>
            Enter your email address and we&apos;ll send you a verification code to reset your password.
          </p>

          <div className={styles.fieldGroup}>
            <label htmlFor="localTenantSlug" className={styles.label}>
              Company Code
            </label>
            <input
              id="localTenantSlug"
              type="text"
              required
              className={styles.input}
              placeholder="Enter Company Code"
              value={localTenantSlug}
              onChange={(e) => setLocalTenantSlug(e.target.value)}
              disabled={isLoading}
            />
          </div>

          <div className={styles.fieldGroup}>
            <label htmlFor="email" className={styles.label}>
              Email Address
            </label>
            <input
              id="email"
              type="email"
              required
              className={styles.input}
              placeholder="Enter your email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={isLoading}
            />
          </div>

          <div className={styles.actions}>
            <button
              type="button"
              onClick={onClose}
              className={styles.cancelButton}
              disabled={isLoading}
            >
              Cancel
            </button>
            <button
              type="submit"
              className={styles.submitButton}
              disabled={isLoading}
            >
              {isLoading ? 'Sending...' : 'Send Verification Code'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

