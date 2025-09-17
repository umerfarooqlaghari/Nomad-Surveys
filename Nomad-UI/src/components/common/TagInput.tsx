'use client';

import React, { useState, useRef, useEffect } from 'react';
import styles from './TagInput.module.css';

interface TagInputProps {
  label: string;
  placeholder?: string;
  tags: string[];
  onTagsChange: (tags: string[]) => void;
  onValidate?: (tag: string) => Promise<boolean>;
  maxTags?: number;
  disabled?: boolean;
  className?: string;
}

export const TagInput: React.FC<TagInputProps> = ({
  label,
  placeholder = 'Type and press Enter to add...',
  tags,
  onTagsChange,
  onValidate,
  maxTags,
  disabled = false,
  className = '',
}) => {
  const [inputValue, setInputValue] = useState('');
  const [isValidating, setIsValidating] = useState(false);
  const [validationStatus, setValidationStatus] = useState<{ [key: string]: 'valid' | 'invalid' | 'pending' }>({});
  const inputRef = useRef<HTMLInputElement>(null);

  // Validate existing tags when onValidate function changes
  useEffect(() => {
    if (onValidate && tags.length > 0) {
      validateExistingTags();
    }
  }, [onValidate]);

  const validateExistingTags = async () => {
    if (!onValidate) return;

    const newValidationStatus: { [key: string]: 'valid' | 'invalid' | 'pending' } = {};
    
    for (const tag of tags) {
      newValidationStatus[tag] = 'pending';
      setValidationStatus(prev => ({ ...prev, [tag]: 'pending' }));
      
      try {
        const isValid = await onValidate(tag);
        newValidationStatus[tag] = isValid ? 'valid' : 'invalid';
      } catch (error) {
        console.error('Validation error for tag:', tag, error);
        newValidationStatus[tag] = 'invalid';
      }
    }
    
    setValidationStatus(prev => ({ ...prev, ...newValidationStatus }));
  };

  const handleInputKeyDown = async (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      await addTag();
    } else if (e.key === 'Backspace' && inputValue === '' && tags.length > 0) {
      removeTag(tags.length - 1);
    }
  };

  const addTag = async () => {
    const trimmedValue = inputValue.trim();
    
    if (!trimmedValue) return;
    
    if (tags.includes(trimmedValue)) {
      setInputValue('');
      return;
    }

    if (maxTags && tags.length >= maxTags) {
      setInputValue('');
      return;
    }

    // Add tag immediately
    const newTags = [...tags, trimmedValue];
    onTagsChange(newTags);
    setInputValue('');

    // Validate if validation function is provided
    if (onValidate) {
      setIsValidating(true);
      setValidationStatus(prev => ({ ...prev, [trimmedValue]: 'pending' }));
      
      try {
        const isValid = await onValidate(trimmedValue);
        setValidationStatus(prev => ({ ...prev, [trimmedValue]: isValid ? 'valid' : 'invalid' }));
      } catch (error) {
        console.error('Validation error:', error);
        setValidationStatus(prev => ({ ...prev, [trimmedValue]: 'invalid' }));
      } finally {
        setIsValidating(false);
      }
    }
  };

  const removeTag = (index: number) => {
    const tagToRemove = tags[index];
    const newTags = tags.filter((_, i) => i !== index);
    onTagsChange(newTags);
    
    // Remove validation status for removed tag
    setValidationStatus(prev => {
      const newStatus = { ...prev };
      delete newStatus[tagToRemove];
      return newStatus;
    });
  };

  const getTagClassName = (tag: string) => {
    const status = validationStatus[tag];
    let className = styles.tag;
    
    if (status === 'valid') {
      className += ` ${styles.tagValid}`;
    } else if (status === 'invalid') {
      className += ` ${styles.tagInvalid}`;
    } else if (status === 'pending') {
      className += ` ${styles.tagPending}`;
    }
    
    return className;
  };

  const getTagTitle = (tag: string) => {
    const status = validationStatus[tag];
    if (status === 'valid') return 'Valid Employee ID';
    if (status === 'invalid') return 'Invalid Employee ID - not found in system';
    if (status === 'pending') return 'Validating...';
    return tag;
  };

  return (
    <div className={`${styles.container} ${className}`}>
      <label className={styles.label}>
        {label}
        {maxTags && (
          <span className={styles.counter}>
            ({tags.length}/{maxTags})
          </span>
        )}
      </label>
      
      <div className={`${styles.inputContainer} ${disabled ? styles.disabled : ''}`}>
        {tags.map((tag, index) => (
          <span
            key={index}
            className={getTagClassName(tag)}
            title={getTagTitle(tag)}
          >
            {tag}
            {!disabled && (
              <button
                type="button"
                className={styles.removeButton}
                onClick={() => removeTag(index)}
                aria-label={`Remove ${tag}`}
              >
                ×
              </button>
            )}
          </span>
        ))}
        
        {!disabled && (!maxTags || tags.length < maxTags) && (
          <input
            ref={inputRef}
            type="text"
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            onKeyDown={handleInputKeyDown}
            onBlur={addTag}
            placeholder={tags.length === 0 ? placeholder : ''}
            className={styles.input}
            disabled={isValidating}
          />
        )}
      </div>
      
      {onValidate && (
        <div className={styles.legend}>
          <span className={styles.legendItem}>
            <span className={`${styles.legendDot} ${styles.legendValid}`}></span>
            Valid
          </span>
          <span className={styles.legendItem}>
            <span className={`${styles.legendDot} ${styles.legendInvalid}`}></span>
            Invalid
          </span>
          <span className={styles.legendItem}>
            <span className={`${styles.legendDot} ${styles.legendPending}`}></span>
            Validating
          </span>
        </div>
      )}
    </div>
  );
};

export default TagInput;
