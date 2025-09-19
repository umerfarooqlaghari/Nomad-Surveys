'use client';

import React, { useState, useRef, useEffect } from 'react';
import styles from './RelationshipTagInput.module.css';

export interface RelationshipTag {
  employeeId: string;
  relationship: string;
}

interface RelationshipTagInputProps {
  label: string;
  placeholder?: string;
  tags: RelationshipTag[];
  onTagsChange: (tags: RelationshipTag[]) => void;
  onValidate?: (employeeIds: string | string[]) => Promise<string[]>;
  className?: string;
  relationshipOptions?: string[];
}

export default function RelationshipTagInput({
  label,
  placeholder = "Enter Employee ID(s), comma-separated...",
  tags,
  onTagsChange,
  onValidate,
  className = '',
  relationshipOptions = ['manager', 'peer', 'subordinate', 'lead', 'trainee', 'mentor', 'other']
}: RelationshipTagInputProps) {
  const [inputValue, setInputValue] = useState('');
  const [selectedRelationship, setSelectedRelationship] = useState(relationshipOptions[0]);
  const [validationStates, setValidationStates] = useState<Record<string, 'pending' | 'valid' | 'invalid'>>({});
  const inputRef = useRef<HTMLInputElement>(null);

  const handleKeyDown = async (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && inputValue.trim()) {
      e.preventDefault();
      await addMultipleTags(inputValue.trim(), selectedRelationship);
    }
  };

  const addMultipleTags = async (input: string, relationship: string) => {
    // Split by comma and filter out empty IDs
    const allEmployeeIds = input.split(',')
      .map(id => id.trim())
      .filter(id => id.length > 0);

    if (allEmployeeIds.length === 0) {
      setInputValue('');
      return;
    }

    // Batch validate all IDs together if validation function is provided
    if (onValidate) {
      // Set all IDs to pending state (including existing ones for visual feedback)
      const pendingStates: Record<string, 'pending'> = {};
      allEmployeeIds.forEach(id => {
        pendingStates[id] = 'pending';
      });
      setValidationStates(prev => ({ ...prev, ...pendingStates }));

      try {
        // Call validation with all IDs at once
        const validIds = await onValidate(allEmployeeIds);
        console.log('All Employee IDs:', allEmployeeIds);
        console.log('Valid IDs from validation:', validIds);
        console.log('Existing tags:', tags);

        // Update validation states and add valid tags
        const validationResults: Record<string, 'valid' | 'invalid'> = {};
        const validTags: RelationshipTag[] = [];

        allEmployeeIds.forEach(employeeId => {
          const isValid = validIds.includes(employeeId);
          const alreadyExists = tags.some(tag => tag.employeeId === employeeId);

          console.log(`Processing ${employeeId}: isValid=${isValid}, alreadyExists=${alreadyExists}`);

          validationResults[employeeId] = isValid ? 'valid' : 'invalid';

          // Only add if valid and doesn't already exist
          if (isValid && !alreadyExists) {
            validTags.push({ employeeId, relationship });
            console.log(`Added ${employeeId} to validTags`);
          }
        });

        console.log('Valid tags to add:', validTags);

        setValidationStates(prev => ({ ...prev, ...validationResults }));

        // Add all valid tags at once
        if (validTags.length > 0) {
          onTagsChange([...tags, ...validTags]);
        }
      } catch (error) {
        console.error('Batch validation error:', error);
        // Mark all as invalid on error
        const errorStates: Record<string, 'invalid'> = {};
        allEmployeeIds.forEach(id => {
          errorStates[id] = 'invalid';
        });
        setValidationStates(prev => ({ ...prev, ...errorStates }));
      }
    } else {
      // No validation - filter out existing and add new tags directly
      const newEmployeeIds = allEmployeeIds.filter(id => !tags.some(tag => tag.employeeId === id));
      const newTags = newEmployeeIds.map(employeeId => ({ employeeId, relationship }));
      onTagsChange([...tags, ...newTags]);
    }

    setInputValue('');
  };

  const addTag = async (employeeId: string, relationship: string) => {
    // Check if tag already exists
    if (tags.some(tag => tag.employeeId === employeeId)) {
      return;
    }

    // Validate if validation function is provided
    if (onValidate) {
      setValidationStates(prev => ({ ...prev, [employeeId]: 'pending' }));

      try {
        const validIds = await onValidate([employeeId]);
        const isValid = validIds.includes(employeeId);
        setValidationStates(prev => ({ ...prev, [employeeId]: isValid ? 'valid' : 'invalid' }));

        if (!isValid) {
          return;
        }
      } catch (error) {
        setValidationStates(prev => ({ ...prev, [employeeId]: 'invalid' }));
        return;
      }
    }

    // Add the tag
    const newTag: RelationshipTag = { employeeId, relationship };
    onTagsChange([...tags, newTag]);
    setInputValue('');
  };

  const removeTag = (employeeId: string) => {
    onTagsChange(tags.filter(tag => tag.employeeId !== employeeId));
    setValidationStates(prev => {
      const newStates = { ...prev };
      delete newStates[employeeId];
      return newStates;
    });
  };

  const updateTagRelationship = (employeeId: string, newRelationship: string) => {
    const updatedTags = tags.map(tag => 
      tag.employeeId === employeeId 
        ? { ...tag, relationship: newRelationship }
        : tag
    );
    onTagsChange(updatedTags);
  };

  const getValidationClass = (employeeId: string) => {
    const state = validationStates[employeeId];
    switch (state) {
      case 'pending': return styles.tagPending;
      case 'valid': return styles.tagValid;
      case 'invalid': return styles.tagInvalid;
      default: return '';
    }
  };

  return (
    <div className={`${styles.container} ${className}`}>
      <label className={styles.label}>{label}</label>
      
      {/* Input Section */}
      <div className={styles.inputSection}>
        <input
          ref={inputRef}
          type="text"
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          className={styles.input}
        />
        <select
          value={selectedRelationship}
          onChange={(e) => setSelectedRelationship(e.target.value)}
          className={styles.relationshipSelect}
        >
          {relationshipOptions.map(option => (
            <option key={option} value={option}>
              {option.charAt(0).toUpperCase() + option.slice(1)}
            </option>
          ))}
        </select>
        <button
          type="button"
          onClick={() => inputValue.trim() && addMultipleTags(inputValue.trim(), selectedRelationship)}
          className={styles.addButton}
          disabled={!inputValue.trim()}
        >
          Add
        </button>
      </div>

      {/* Tags Display */}
      <div className={styles.tagsContainer}>
        {tags.map((tag) => (
          <div
            key={tag.employeeId}
            className={`${styles.tag} ${getValidationClass(tag.employeeId)}`}
          >
            <span className={styles.tagEmployeeId}>{tag.employeeId}</span>
            <select
              value={tag.relationship}
              onChange={(e) => updateTagRelationship(tag.employeeId, e.target.value)}
              className={styles.tagRelationshipSelect}
              onClick={(e) => e.stopPropagation()}
            >
              {relationshipOptions.map(option => (
                <option key={option} value={option}>
                  {option.charAt(0).toUpperCase() + option.slice(1)}
                </option>
              ))}
            </select>
            <button
              type="button"
              onClick={() => removeTag(tag.employeeId)}
              className={styles.removeButton}
              aria-label={`Remove ${tag.employeeId}`}
            >
              ×
            </button>
          </div>
        ))}
      </div>

      {/* Validation Legend */}
      {onValidate && (
        <div className={styles.legend}>
          <span className={styles.legendItem}>
            <span className={`${styles.legendColor} ${styles.legendValid}`}></span>
            Valid
          </span>
          <span className={styles.legendItem}>
            <span className={`${styles.legendColor} ${styles.legendPending}`}></span>
            Validating...
          </span>
          <span className={styles.legendItem}>
            <span className={`${styles.legendColor} ${styles.legendInvalid}`}></span>
            Invalid
          </span>
        </div>
      )}
    </div>
  );
}
