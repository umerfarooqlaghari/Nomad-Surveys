/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { QuestionType, TenantSettings, RatingOption, DEFAULT_TENANT_SETTINGS } from '@/types/survey';
import { toast } from 'react-hot-toast';
import styles from './SurveySettingsTab.module.css';

interface TenantSettingsTabProps {
  tenantSlug: string;
  token?: string;
}

const QUESTION_TYPES: { value: QuestionType; label: string }[] = [
  { value: 'rating', label: 'Rating Scale' },
  { value: 'text', label: 'Short Text' },
  { value: 'textarea', label: 'Long Text' },
];

export default function SurveySettingsTab({ tenantSlug, token: tokenProp }: TenantSettingsTabProps) {
  const [settings, setSettings] = useState<TenantSettings | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [defaultQuestionType, setDefaultQuestionType] = useState<QuestionType>('rating');
  const [ratingOptions, setRatingOptions] = useState<RatingOption[]>(DEFAULT_TENANT_SETTINGS.defaultRatingOptions || []);
  const [numberOfOptions, setNumberOfOptions] = useState<number>(5);

  useEffect(() => {
    loadSettings();
    console.log('1');
  }, []);

  const loadSettings = async () => {
    try {
      setIsLoading(true);
      // Use token from props or fallback to localStorage
      const authToken = tokenProp || localStorage.getItem('token');

      console.log('🔑 [SurveySettingsTab] Loading settings with token:', authToken ? 'Present' : 'Missing');

      if (!authToken) {
        console.warn('⚠️ [SurveySettingsTab] No token available');
        toast.error('Authentication required');
        setIsLoading(false);
        return;
      }

      const response = await fetch(`/api/${tenantSlug}/settings`, {
        headers: {
          'Authorization': `Bearer ${authToken}`,
        },
      });

      console.log('📡 [SurveySettingsTab] Settings API response:', response.status, response.statusText);

      if (response.ok) {
        const data = await response.json();
        console.log('📦 [SurveySettingsTab] Settings data (raw from API):', data);

        // If data is null, settings don't exist yet - use defaults
        if (data === null) {
          console.log('⚠️ [SurveySettingsTab] No settings found, using defaults');
          setSettings(null);
          setDefaultQuestionType(DEFAULT_TENANT_SETTINGS.defaultQuestionType);
          setRatingOptions(DEFAULT_TENANT_SETTINGS.defaultRatingOptions || []);
          setNumberOfOptions(DEFAULT_TENANT_SETTINGS.numberOfOptions || 5);
        } else {
          // Transform PascalCase to camelCase
          const transformedSettings: TenantSettings = {
            id: data.Id,
            tenantId: data.TenantId,
            defaultQuestionType: data.DefaultQuestionType,
            defaultRatingOptions: data.DefaultRatingOptions?.map((opt: any) => ({
              id: opt.Id || opt.id,
              text: opt.Text || opt.text,
              order: opt.Order ?? opt.order,
              score: opt.Score ?? opt.score ?? 0,
            })),
            numberOfOptions: data.NumberOfOptions,
          };
          console.log('✅ [SurveySettingsTab] Settings loaded and transformed:', transformedSettings);

          setSettings(transformedSettings);
          setDefaultQuestionType(transformedSettings.defaultQuestionType);
          setRatingOptions(transformedSettings.defaultRatingOptions || DEFAULT_TENANT_SETTINGS.defaultRatingOptions || []);
          setNumberOfOptions(transformedSettings.numberOfOptions || 5);
        }
      } else {
        toast.error('Failed to load tenant settings');
      }
    } catch (error) {
      console.error('Error loading tenant settings:', error);
      toast.error('Failed to load tenant settings');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSaveSettings = async () => {
    try {
      setIsSaving(true);
      // Use token from props or fallback to localStorage
      const authToken = tokenProp || localStorage.getItem('token');

      if (!authToken) {
        toast.error('Authentication required');
        setIsSaving(false);
        return;
      }

      const payload = {
        defaultQuestionType,
        defaultRatingOptions: defaultQuestionType === 'rating' ? ratingOptions : undefined,
        numberOfOptions: defaultQuestionType === 'rating' ? numberOfOptions : undefined,
      };

      console.log('💾 [SurveySettingsTab] Saving settings:', {
        payload,
        ratingOptionsCount: ratingOptions.length,
        numberOfOptions,
      });

      const method = settings ? 'PUT' : 'POST';
      const response = await fetch(`/api/${tenantSlug}/settings`, {
        method,
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        const data = await response.json();
        console.log('✅ [SurveySettingsTab] Settings saved, response:', data);
        setSettings(data);
        toast.success('Tenant settings saved successfully');
      } else {
        const error = await response.json();
        toast.error(error.error || 'Failed to save tenant settings');
      }
    } catch (error) {
      console.error('Error saving tenant settings:', error);
      toast.error('Failed to save survey settings');
    } finally {
      setIsSaving(false);
    }
  };

  const handleAddOption = () => {
    const newOption: RatingOption = {
      id: Date.now().toString(),
      text: '',
      order: ratingOptions.length,
      score: ratingOptions.length > 0 ? (ratingOptions[ratingOptions.length - 1].score ?? ratingOptions.length) + 1 : 1,
    };
    setRatingOptions([...ratingOptions, newOption]);
    setNumberOfOptions(ratingOptions.length + 1);
  };

  const handleUpdateOption = (id: string, text: string) => {
    setRatingOptions(ratingOptions.map(opt =>
      opt.id === id ? { ...opt, text } : opt
    ));
  };

  const handleDeleteOption = (id: string) => {
    const filtered = ratingOptions.filter(opt => opt.id !== id);
    setRatingOptions(filtered.map((opt, index) => ({ ...opt, order: index })));
    setNumberOfOptions(filtered.length);
  };

  const handleMoveOption = (index: number, direction: 'up' | 'down') => {
    const newOptions = [...ratingOptions];
    const targetIndex = direction === 'up' ? index - 1 : index + 1;

    if (targetIndex < 0 || targetIndex >= newOptions.length) return;

    [newOptions[index], newOptions[targetIndex]] = [newOptions[targetIndex], newOptions[index]];
    setRatingOptions(newOptions.map((opt, idx) => ({ ...opt, order: idx })));
  };

  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.loading}>Loading settings...</div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h2 className={styles.title}>Survey Default Settings</h2>
        <p className={styles.description}>
          Configure default settings for questions in this survey. These settings will be applied to new questions added manually or imported.
        </p>
      </div>

      <div className={styles.section}>
        <label className={styles.label}>
          Default Question Type
        </label>
        <select
          value={defaultQuestionType}
          onChange={(e) => setDefaultQuestionType(e.target.value as QuestionType)}
          className={styles.select}
        >
          {QUESTION_TYPES.map((type) => (
            <option key={type.value} value={type.value}>
              {type.label}
            </option>
          ))}
        </select>
      </div>

      {defaultQuestionType === 'rating' && (
        <div className={styles.section}>
          <div className={styles.sectionHeader}>
            <label className={styles.label}>Rating Options</label>
            <button
              type="button"
              onClick={handleAddOption}
              className={styles.addButton}
            >
              + Add Option
            </button>
          </div>

          <div className={styles.optionsList}>
            <div className="flex px-2 text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">
              <div className="w-8">#</div>
              <div className="flex-1">Text</div>
              <div className="w-20 mx-2">Score</div>
              <div className="w-24 text-right">Actions</div>
            </div>
            {ratingOptions.map((option, index) => (
              <div key={option.id} className={styles.optionItem}>
                <div className={styles.optionOrder}>{index + 1}</div>
                <input
                  type="text"
                  value={option.text}
                  onChange={(e) => handleUpdateOption(option.id, e.target.value)}
                  placeholder={`Option ${index + 1}`}
                  className={styles.optionInput}
                />
                <input
                  type="number"
                  value={option.score ?? index + 1}
                  onChange={(e) => {
                    const newScore = parseInt(e.target.value) || 0;
                    setRatingOptions(ratingOptions.map(opt =>
                      opt.id === option.id ? { ...opt, score: newScore } : opt
                    ));
                  }}
                  className="w-20 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black text-center mx-2"
                  title="Score value"
                />
                <div className={styles.optionActions}>
                  <button
                    type="button"
                    onClick={() => handleMoveOption(index, 'up')}
                    disabled={index === 0}
                    className={styles.moveButton}
                    title="Move up"
                  >
                    ↑
                  </button>
                  <button
                    type="button"
                    onClick={() => handleMoveOption(index, 'down')}
                    disabled={index === ratingOptions.length - 1}
                    className={styles.moveButton}
                    title="Move down"
                  >
                    ↓
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDeleteOption(option.id)}
                    disabled={ratingOptions.length <= 2}
                    className={styles.deleteButton}
                    title="Delete option"
                  >
                    ×
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className={styles.optionsInfo}>
            Total options: {numberOfOptions}
          </div>
        </div>
      )}

      <div className={styles.footer}>
        <button
          onClick={handleSaveSettings}
          disabled={isSaving}
          className={styles.saveButton}
        >
          {isSaving ? 'Saving...' : 'Save Settings'}
        </button>
      </div>
    </div>
  );
}

