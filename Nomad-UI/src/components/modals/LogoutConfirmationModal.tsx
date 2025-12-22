'use client';

import React, { useEffect } from 'react';
import styles from './LogoutConfirmationModal.module.css';

interface LogoutConfirmationModalProps {
  isOpen: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export default function LogoutConfirmationModal({
  isOpen,
  onConfirm,
  onCancel,
}: LogoutConfirmationModalProps) {
  // Prevent body scroll when modal is open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }

    // Cleanup on unmount
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen]);

  if (!isOpen) return null;

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      onCancel();
    }
  };

  return (
    <div className={styles.backdrop} onClick={handleBackdropClick}>
      <div className={styles.modal}>
        <div className={styles.header}>
          <h2 className={styles.title}>Confirm Logout</h2>
          <button
            onClick={onCancel}
            className={styles.closeButton}
            aria-label="Close"
          >
            <svg className={styles.closeIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div className={styles.content}>
          <div className={styles.iconContainer}>
            <svg 
              className={styles.icon} 
              fill="none" 
              stroke="currentColor" 
              viewBox="0 0 24 24"
            >
              <path 
                strokeLinecap="round" 
                strokeLinejoin="round" 
                strokeWidth={2} 
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" 
              />
            </svg>
          </div>
          
          <p className={styles.message}>
            Are you sure you want to logout?
          </p>
          <p className={styles.submessage}>
            You will need to sign in again to access your account.
          </p>
        </div>

        <div className={styles.footer}>
          <button
            onClick={onCancel}
            className={styles.cancelButton}
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            className={styles.confirmButton}
          >
            Yes, I&apos;m Sure
          </button>
        </div>
      </div>
    </div>
  );
}

