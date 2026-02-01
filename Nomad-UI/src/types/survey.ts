/**
 * Custom Survey Builder Type Definitions
 * 
 * This file defines the schema and types for the custom survey builder
 * that replaces the SurveyJS implementation.
 */

export type QuestionType =
  | 'rating'
  | 'text'
  | 'textarea';


export interface ChoiceOption {
  id: string;
  text: string;
  order: number;
  score: number;
  value?: number | string; // Value stored in the response (usually score for backend)
}

export interface QuestionConfig {
  // Rating Scale Configuration
  ratingMin?: number;
  ratingMax?: number;
  ratingStep?: number;
  ratingLabels?: {
    min: string;
    max: string;
  };
  ratingOptions?: ChoiceOption[]; // Custom rating options with scores

  // Multiple Choice / Dropdown Configuration
  options?: ChoiceOption[];

  // Multiple Choice (Multi-select) Configuration
  minSelections?: number;
  maxSelections?: number;

  // Text Input Configuration
  maxLength?: number;
  minLength?: number;
  placeholder?: string;

  // Number Input Configuration
  numberMin?: number;
  numberMax?: number;
}

export interface Question {
  id: string;
  name: string; // Unique identifier for the question (auto-generated)
  type: QuestionType;
  selfText: string; // Question text shown to self-evaluators
  othersText: string; // Question text shown to others (managers, peers, etc.)
  description?: string;
  required: boolean;
  order: number;
  config: QuestionConfig;
  showTo?: 'everyone' | 'self' | 'others'; // Visibility control

  // Metadata for imported questions
  importedFrom?: {
    questionId: string;
    clusterId: string;
    competencyId: string;
  };
}

export interface SurveyPage {
  id: string;
  name: string; // Internal name (e.g., "page1", "page2")
  title: string; // Display title (e.g., "Introduction", "Integrity")
  description?: string;
  questions: Question[];
  order: number;
}

export interface SurveySchema {
  id?: string;
  title: string;
  description?: string;
  pages: SurveyPage[];
  createdAt?: string;
  updatedAt?: string;
}

// Helper type for question type display names
export const QUESTION_TYPE_LABELS: Record<QuestionType, string> = {
  'rating': 'Rating Scale',
  'text': 'Short Text',
  'textarea': 'Long Text',
};

// Default configurations for each question type
export const DEFAULT_QUESTION_CONFIGS: Record<QuestionType, QuestionConfig> = {
  'rating': {
    ratingMin: 1,
    ratingMax: 5,
    ratingStep: 1,
    ratingLabels: { min: 'Never', max: 'Always' },
  },
  // 'single-choice': {
  //   options: [
  //     { id: 'opt1', text: 'Option 1', order: 0, score: 1 },
  //     { id: 'opt2', text: 'Option 2', order: 1, score: 2 },
  //   ],
  // },
  // 'multiple-choice': {
  //   options: [
  //     { id: 'opt1', text: 'Option 1', order: 0, score: 1 },
  //     { id: 'opt2', text: 'Option 2', order: 1, score: 2 },
  //   ],
  //   minSelections: 0,
  //   maxSelections: undefined,
  // },
  'text': {
    maxLength: 500,
    placeholder: 'Enter your answer...',
  },
  'textarea': {
    maxLength: 2000,
    placeholder: 'Enter your answer...',
  },
  // 'dropdown': {
  //   options: [
  //     { id: 'opt1', text: 'Option 1', order: 0, score: 1 },
  //     { id: 'opt2', text: 'Option 2', order: 1, score: 2 },
  //   ],
  // },
};

// Validation helpers
export interface ValidationError {
  field: string;
  message: string;
}

export function validateQuestion(question: Question): ValidationError[] {
  const errors: ValidationError[] = [];

  // Self text validation: Optional only for text/textarea types
  const isTextType = ['text', 'textarea'].includes(question.type);
  if (!isTextType && !question.selfText.trim()) {
    errors.push({ field: 'selfText', message: 'Self question text is required' });
  }

  if (!question.othersText.trim()) {
    errors.push({ field: 'othersText', message: 'Others question text is required' });
  }

  // Type-specific validation
  if (question.type === 'rating') {
    const { ratingMin, ratingMax } = question.config;
    if (ratingMin !== undefined && ratingMax !== undefined && ratingMin >= ratingMax) {
      errors.push({ field: 'config.rating', message: 'Minimum must be less than maximum' });
    }
  }

  if (['single-choice', 'multiple-choice', 'dropdown'].includes(question.type)) {
    if (!question.config.options || question.config.options.length < 2) {
      errors.push({ field: 'config.options', message: 'At least 2 options are required' });
    }
  }

  return errors;
}

export function validateSurvey(survey: SurveySchema): ValidationError[] {
  const errors: ValidationError[] = [];

  if (!survey.title.trim()) {
    errors.push({ field: 'title', message: 'Survey title is required' });
  }

  if (survey.pages.length === 0) {
    errors.push({ field: 'pages', message: 'Survey must have at least one page' });
  }

  survey.pages.forEach((page, pageIndex) => {
    if (!page.title.trim()) {
      errors.push({ field: `pages[${pageIndex}].title`, message: 'Page title is required' });
    }

    if (page.questions.length === 0) {
      errors.push({ field: `pages[${pageIndex}].questions`, message: 'Page must have at least one question' });
    }

    page.questions.forEach((question, questionIndex) => {
      const questionErrors = validateQuestion(question);
      questionErrors.forEach(err => {
        errors.push({
          field: `pages[${pageIndex}].questions[${questionIndex}].${err.field}`,
          message: err.message,
        });
      });
    });
  });

  return errors;
}

// Utility functions
export function generateQuestionId(): string {
  return `q_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
}

export function generatePageId(): string {
  return `page_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
}

export function generateOptionId(): string {
  return `opt_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
}

export function createDefaultQuestion(type: QuestionType): Question {
  return {
    id: generateQuestionId(),
    name: `question_${Date.now()}`,
    type,
    selfText: '',
    othersText: '',
    required: false,
    order: 0,
    config: { ...DEFAULT_QUESTION_CONFIGS[type] },
  };
}

export function createDefaultPage(): SurveyPage {
  return {
    id: generatePageId(),
    name: `page_${Date.now()}`,
    title: 'New Page',
    questions: [],
    order: 0,
  };
}

export function createDefaultSurvey(): SurveySchema {
  const firstPage = createDefaultPage();
  firstPage.title = 'Page 1';

  return {
    title: 'New Survey',
    description: '',
    pages: [firstPage],
  };
}

// Survey Settings Types
export interface RatingOption {
  id: string;
  text: string;
  order: number;
  score: number;
}

export interface TenantSettings {
  id?: string;
  tenantId?: string;
  defaultQuestionType: QuestionType;
  defaultRatingOptions?: RatingOption[];
  numberOfOptions?: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface UpdateTenantSettingsRequest {
  defaultQuestionType: QuestionType;
  defaultRatingOptions?: RatingOption[];
  numberOfOptions?: number;
}

export const DEFAULT_TENANT_SETTINGS: Omit<TenantSettings, 'id' | 'tenantId'> = {
  defaultQuestionType: 'rating',
  defaultRatingOptions: [
    { id: '1', text: 'Never', order: 0, score: 1 },
    { id: '2', text: 'Sometimes', order: 1, score: 2 },
    { id: '3', text: 'Often', order: 2, score: 3 },
    { id: '4', text: 'Always', order: 3, score: 4 },
    { id: '5', text: 'N/A', order: 4, score: 0 },
  ],
  numberOfOptions: 5,
};

