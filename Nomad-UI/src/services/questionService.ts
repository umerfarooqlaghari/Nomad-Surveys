/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, handleApiResponse } from './api';
import {
  ClusterResponse,
  ClusterListResponse,
  CreateClusterRequest,
  UpdateClusterRequest,
  CompetencyResponse,
  CompetencyListResponse,
  CreateCompetencyRequest,
  UpdateCompetencyRequest,
  QuestionResponse,
  QuestionListResponse,
  CreateQuestionRequest,
  UpdateQuestionRequest,
} from '@/types/questions';

class QuestionService {
  // ==================== CLUSTER METHODS ====================

  /**
   * Get all clusters for a tenant
   */
  async getClusters(
    tenantSlug: string,
    token: string
  ): Promise<{ data: ClusterListResponse[] | null; error: string | null }> {
    try {
      const response = await apiClient.get<ClusterListResponse[]>(
        `/${tenantSlug}/clusters`,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to fetch clusters' };
    }
  }

  /**
   * Get a specific cluster by ID with nested data
   */
  async getClusterById(
    tenantSlug: string,
    clusterId: string,
    token: string
  ): Promise<{ data: ClusterResponse | null; error: string | null }> {
    try {
      const response = await apiClient.get<ClusterResponse>(
        `/${tenantSlug}/clusters/${clusterId}`,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to fetch cluster' };
    }
  }

  /**
   * Create a new cluster
   */
  async createCluster(
    tenantSlug: string,
    clusterData: CreateClusterRequest,
    token: string
  ): Promise<{ data: ClusterResponse | null; error: string | null }> {
    try {
      const response = await apiClient.post<ClusterResponse>(
        `/${tenantSlug}/clusters`,
        clusterData,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to create cluster' };
    }
  }

  /**
   * Update an existing cluster
   */
  async updateCluster(
    tenantSlug: string,
    clusterId: string,
    clusterData: UpdateClusterRequest,
    token: string
  ): Promise<{ data: ClusterResponse | null; error: string | null }> {
    try {
      const response = await apiClient.put<ClusterResponse>(
        `/${tenantSlug}/clusters/${clusterId}`,
        clusterData,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to update cluster' };
    }
  }

  /**
   * Delete a cluster (soft delete)
   */
  async deleteCluster(
    tenantSlug: string,
    clusterId: string,
    token: string
  ): Promise<{ success: boolean; error: string | null }> {
    try {
      const response = await apiClient.delete(
        `/${tenantSlug}/clusters/${clusterId}`,
        token
      );
      if (response.status === 204) {
        return { success: true, error: null };
      }
      return { success: false, error: response.error || 'Failed to delete cluster' };
    } catch (error: any) {
      return { success: false, error: error.message || 'Failed to delete cluster' };
    }
  }

  // ==================== COMPETENCY METHODS ====================

  /**
   * Get all competencies for a tenant, optionally filtered by cluster
   */
  async getCompetencies(
    tenantSlug: string,
    token: string,
    clusterId?: string
  ): Promise<{ data: CompetencyListResponse[] | null; error: string | null }> {
    try {
      const url = clusterId
        ? `/${tenantSlug}/competencies?clusterId=${clusterId}`
        : `/${tenantSlug}/competencies`;

      const response = await apiClient.get<CompetencyListResponse[]>(url, token);
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to fetch competencies' };
    }
  }

  /**
   * Get a specific competency by ID with nested data
   */
  async getCompetencyById(
    tenantSlug: string,
    competencyId: string,
    token: string
  ): Promise<{ data: CompetencyResponse | null; error: string | null }> {
    try {
      const response = await apiClient.get<CompetencyResponse>(
        `/${tenantSlug}/competencies/${competencyId}`,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to fetch competency' };
    }
  }

  /**
   * Create a new competency
   */
  async createCompetency(
    tenantSlug: string,
    competencyData: CreateCompetencyRequest,
    token: string
  ): Promise<{ data: CompetencyResponse | null; error: string | null }> {
    try {
      const response = await apiClient.post<CompetencyResponse>(
        `/${tenantSlug}/competencies`,
        competencyData,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to create competency' };
    }
  }

  /**
   * Update an existing competency
   */
  async updateCompetency(
    tenantSlug: string,
    competencyId: string,
    competencyData: UpdateCompetencyRequest,
    token: string
  ): Promise<{ data: CompetencyResponse | null; error: string | null }> {
    try {
      const response = await apiClient.put<CompetencyResponse>(
        `/${tenantSlug}/competencies/${competencyId}`,
        competencyData,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to update competency' };
    }
  }

  /**
   * Delete a competency (soft delete)
   */
  async deleteCompetency(
    tenantSlug: string,
    competencyId: string,
    token: string
  ): Promise<{ success: boolean; error: string | null }> {
    try {
      const response = await apiClient.delete(
        `/${tenantSlug}/competencies/${competencyId}`,
        token
      );
      if (response.status === 204) {
        return { success: true, error: null };
      }
      return { success: false, error: response.error || 'Failed to delete competency' };
    } catch (error: any) {
      return { success: false, error: error.message || 'Failed to delete competency' };
    }
  }

  // ==================== QUESTION METHODS ====================

  /**
   * Get all questions for a tenant, optionally filtered by competency
   */
  async getQuestions(
    tenantSlug: string,
    token: string,
    competencyId?: string
  ): Promise<{ data: QuestionListResponse[] | null; error: string | null }> {
    try {
      const url = competencyId
        ? `/${tenantSlug}/questions?competencyId=${competencyId}`
        : `/${tenantSlug}/questions`;

      const response = await apiClient.get<QuestionListResponse[]>(url, token);
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to fetch questions' };
    }
  }

  /**
   * Get a specific question by ID
   */
  async getQuestionById(
    tenantSlug: string,
    questionId: string,
    token: string
  ): Promise<{ data: QuestionResponse | null; error: string | null }> {
    try {
      const response = await apiClient.get<QuestionResponse>(
        `/${tenantSlug}/questions/${questionId}`,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to fetch question' };
    }
  }

  /**
   * Create a new question
   */
  async createQuestion(
    tenantSlug: string,
    questionData: CreateQuestionRequest,
    token: string
  ): Promise<{ data: QuestionResponse | null; error: string | null }> {
    try {
      const response = await apiClient.post<QuestionResponse>(
        `/${tenantSlug}/questions`,
        questionData,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to create question' };
    }
  }

  /**
   * Update an existing question
   */
  async updateQuestion(
    tenantSlug: string,
    questionId: string,
    questionData: UpdateQuestionRequest,
    token: string
  ): Promise<{ data: QuestionResponse | null; error: string | null }> {
    try {
      const response = await apiClient.put<QuestionResponse>(
        `/${tenantSlug}/questions/${questionId}`,
        questionData,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to update question' };
    }
  }

  /**
   * Delete a question (soft delete)
   */
  async deleteQuestion(
    tenantSlug: string,
    questionId: string,
    token: string
  ): Promise<{ success: boolean; error: string | null }> {
    try {
      const response = await apiClient.delete(
        `/${tenantSlug}/questions/${questionId}`,
        token
      );
      if (response.status === 204) {
        return { success: true, error: null };
      }
      return { success: false, error: response.error || 'Failed to delete question' };
    } catch (error: any) {
      return { success: false, error: error.message || 'Failed to delete question' };
    }
  }

  /**
   * Upload a question bank (Excel file)
   */
  async uploadQuestionBank(
    tenantSlug: string,
    file: File,
    token: string
  ): Promise<{ data: any | null; error: string | null }> {
    try {
      const formData = new FormData();
      formData.append('file', file);

      // Use native fetch to handle FormData and file upload
      const response = await fetch(`${apiClient.baseURL}/${tenantSlug}/questions/upload-question-bank`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        },
        body: formData
      });

      if (!response.ok) {
        const errorData = await response.json();
        return { data: null, error: errorData.error || 'Failed to upload question bank' };
      }

      const data = await response.json();
      return { data, error: null };
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to upload question bank' };
    }
  }

  /**
   * Download the question bank template
   */
  async downloadTemplate(
    tenantSlug: string,
    token: string
  ): Promise<{ error: string | null }> {
    try {
      const response = await fetch(`${apiClient.baseURL}/${tenantSlug}/questions/download-template`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!response.ok) {
        const errorData = await response.json();
        return { error: errorData.error || 'Failed to download template' };
      }

      // Handle file download
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'QuestionBank_Template.xlsx';
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      return { error: null };
    } catch (error: any) {
      return { error: error.message || 'Failed to download template' };
    }
  }
}

export const questionService = new QuestionService();

