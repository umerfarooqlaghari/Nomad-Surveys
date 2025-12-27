'use client';

import React, { useEffect, useState } from 'react';
import styles from './DeleteConfirmationModal.module.css';

interface DeleteConfirmationModalProps {
    isOpen: boolean;
    onConfirm: () => void;
    onCancel: () => void;
    companyName: string;
}

export default function DeleteConfirmationModal({
    isOpen,
    onConfirm,
    onCancel,
    companyName,
}: DeleteConfirmationModalProps) {
    const [confirmText, setConfirmText] = useState('');

    // Prevent body scroll when modal is open
    useEffect(() => {
        if (isOpen) {
            document.body.style.overflow = 'hidden';
            setConfirmText(''); // Reset on open
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

    const isConfirmed = confirmText.toLowerCase() === 'confirm';

    return (
        <div className={styles.backdrop} onClick={handleBackdropClick}>
            <div className={styles.modal}>
                <div className={styles.header}>
                    <h2 className={styles.title}>Confirm Deletion</h2>
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
                                d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                            />
                        </svg>
                    </div>

                    <p className={styles.message}>
                        Are you sure you want to delete <span style={{ color: '#ef4444' }}>{companyName}</span>?
                    </p>
                    <p className={styles.submessage}>
                        This action will deactivate the company. This is a soft delete and can be reversed by an administrator.
                    </p>

                    <div className={styles.inputGroup}>
                        <label htmlFor="confirmText" className={styles.inputLabel}>
                            Type <strong>Confirm</strong> to proceed
                        </label>
                        <input
                            type="text"
                            id="confirmText"
                            value={confirmText}
                            onChange={(e) => setConfirmText(e.target.value)}
                            className={styles.confirmInput}
                            placeholder='Type "Confirm"'
                            autoFocus
                            autoComplete="off"
                        />
                    </div>
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
                        className={styles.deleteButton}
                        disabled={!isConfirmed}
                    >
                        Yes, Delete Company
                    </button>
                </div>
            </div>
        </div>
    );
}
