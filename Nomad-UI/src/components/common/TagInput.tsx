'use client';

import React, { useState, useRef, useEffect } from 'react';
import styles from './TagInput.module.css';

interface TagInputProps {
  label: string;
  placeholder?: string;
  tags: string[];
  onTagsChange: (tags: string[]) => void;
  onValidate?: (tags: string | string[]) => Promise<string[]>;
  maxTags?: number;
  disabled?: boolean;
  className?: string;
}

export const TagInput: React.FC<TagInputProps> = ({
  label,
  placeholder = 'Type Employee ID(s), comma-separated...',
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
  const validationTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Validate existing tags when tags change (but not when onValidate changes to prevent infinite loops)
  useEffect(() => {
    if (onValidate && tags.length > 0) {
      // Only validate tags that don't have a validation status yet
      const unvalidatedTags = tags.filter(tag => !validationStatus[tag]);
      if (unvalidatedTags.length > 0) {
        validateExistingTags();
      }
    }
  }, [tags]); // Only depend on tags, not onValidate

  const validateExistingTags = async () => {
    if (!onValidate) return;

    // Clear any existing timeout
    if (validationTimeoutRef.current) {
      clearTimeout(validationTimeoutRef.current);
    }

    // Debounce validation to prevent rapid calls
    validationTimeoutRef.current = setTimeout(async () => {
      const newValidationStatus: { [key: string]: 'valid' | 'invalid' | 'pending' } = {};

      for (const tag of tags) {
        // Skip if already validated
        if (validationStatus[tag]) continue;

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
    }, 300); // 300ms debounce
  };

  const handleInputKeyDown = async (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      await addMultipleTags();
    } else if (e.key === 'Backspace' && inputValue === '' && tags.length > 0) {
      removeTag(tags.length - 1);
    }
  };

  const addMultipleTags = async () => {
    const trimmedValue = inputValue.trim();
    if (!trimmedValue) return;

    // Split by comma and filter out empty tags
    const allTagValues = trimmedValue.split(',')
      .map(tag => tag.trim())
      .filter(tag => tag.length > 0);

    if (allTagValues.length === 0) {
      setInputValue('');
      return;
    }

    // Batch validate all tags together if validation function is provided
    if (onValidate) {
      setIsValidating(true);

      // Set all tags to pending state (including existing ones for visual feedback)
      const pendingStates: Record<string, 'pending'> = {};
      allTagValues.forEach(tag => {
        pendingStates[tag] = 'pending';
      });
      setValidationStatus(prev => ({ ...prev, ...pendingStates }));

      try {
        // Call validation with all tags at once
        const validTags = await onValidate(allTagValues);

        // Update validation states and add valid tags
        const validationResults: Record<string, 'valid' | 'invalid'> = {};
        const tagsToAdd: string[] = [];

        allTagValues.forEach(tagValue => {
          const isValid = validTags.includes(tagValue);
          const alreadyExists = tags.includes(tagValue);

          validationResults[tagValue] = isValid ? 'valid' : 'invalid';

          // Only add if valid and doesn't already exist
          if (isValid && !alreadyExists) {
            tagsToAdd.push(tagValue);
          }
        });

        setValidationStatus(prev => ({ ...prev, ...validationResults }));

        // Check max tags limit for new tags only
        if (maxTags && tags.length + tagsToAdd.length > maxTags) {
          setInputValue('');
          setIsValidating(false);
          return;
        }

        // Add all valid tags at once
        if (tagsToAdd.length > 0) {
          onTagsChange([...tags, ...tagsToAdd]);
        }
      } catch (error) {
        console.error('Batch validation error:', error);
        // Mark all as invalid on error
        const errorStates: Record<string, 'invalid'> = {};
        allTagValues.forEach(tag => {
          errorStates[tag] = 'invalid';
        });
        setValidationStatus(prev => ({ ...prev, ...errorStates }));
      } finally {
        setIsValidating(false);
      }
    } else {
      // No validation - filter out existing and add new tags directly
      const newTagValues = allTagValues.filter(tag => !tags.includes(tag));

      // Check max tags limit
      if (maxTags && tags.length + newTagValues.length > maxTags) {
        setInputValue('');
        return;
      }

      onTagsChange([...tags, ...newTagValues]);
    }

    setInputValue('');
  };

  const addTag = async () => {
    const trimmedValue = inputValue.trim();
    if (!trimmedValue) return;

    await addSingleTag(trimmedValue);
    setInputValue('');
  };

  const addSingleTag = async (tagValue: string) => {
    if (!tagValue) return;

    if (tags.includes(tagValue)) {
      return;
    }

    if (maxTags && tags.length >= maxTags) {
      return;
    }

    // Add tag immediately
    const newTags = [...tags, tagValue];
    onTagsChange(newTags);

    // Validate if validation function is provided
    if (onValidate) {
      setIsValidating(true);
      setValidationStatus(prev => ({ ...prev, [tagValue]: 'pending' }));

      try {
        const validTags = await onValidate([tagValue]);
        const isValid = validTags.includes(tagValue);
        setValidationStatus(prev => ({ ...prev, [tagValue]: isValid ? 'valid' : 'invalid' }));
      } catch (error) {
        console.error('Validation error:', error);
        setValidationStatus(prev => ({ ...prev, [tagValue]: 'invalid' }));
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
