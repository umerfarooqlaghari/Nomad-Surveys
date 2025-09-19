/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, handleApiResponse } from './api';

// Types for Subject API
export interface Subject {
  Id: string;
  FirstName: string;
  LastName: string;
  FullName: string;
  Email: string;
  EmployeeId: string;
  CompanyName?: string;
  Gender?: string;
  BusinessUnit?: string;
  Grade?: string;
  Designation?: string;
  Tenure?: number;
  Location?: string;
  Metadata1?: string;
  Metadata2?: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  AssignedEvaluatorIds?: string[];
}

// List response has fewer fields than full subject response
export interface SubjectListResponse {
  Id: string;
  FirstName: string;
  LastName: string;
  FullName: string;
  Email: string;
  EmployeeId: string;
  CompanyName?: string;
  Designation?: string;
  Location?: string;
  IsActive: boolean;
  CreatedAt: string;
  LastLoginAt?: string;
  TenantId: string;
  EvaluatorCount: number;
}

export interface EvaluatorRelationship {
  EvaluatorId: string;
  Relationship: string;
}

export interface ValidationResult {
  EmployeeId: string;
  IsValid: boolean;
  Message?: string;
  Data?: {
    Id: string;
    EmployeeId: string;
    FullName: string;
    Email: string;
    IsActive: boolean;
  };
}

export interface ValidationResponse {
  Results: ValidationResult[];
  TotalRequested: number;
  ValidCount: number;
  InvalidCount: number;
}

export interface CreateSubjectRequest {
  FirstName: string;
  LastName: string;
  Email: string;
  EmployeeId: string;
  CompanyName?: string;
  Gender?: string;
  BusinessUnit?: string;
  Grade?: string;
  Designation?: string;
  Tenure?: number;
  Location?: string;
  Metadata1?: string;
  Metadata2?: string;
  RelatedEmployeeIds?: string[];
  EvaluatorRelationships?: EvaluatorRelationship[];
}

export interface UpdateSubjectRequest {
  FirstName: string;
  LastName: string;
  Email: string;
  EmployeeId: string;
  CompanyName?: string;
  Gender?: string;
  BusinessUnit?: string;
  Grade?: string;
  Designation?: string;
  Tenure?: number;
  Location?: string;
  Metadata1?: string;
  Metadata2?: string;
  RelatedEmployeeIds?: string[];
  EvaluatorRelationships?: EvaluatorRelationship[];
}

export interface BulkCreateSubjectsRequest {
  Subjects: CreateSubjectRequest[];
}

export interface BulkCreateResponse {
  TotalRequested: number;
  SuccessfullyCreated: number;
  UpdatedCount: number;
  Failed: number;
  Errors: string[];
  CreatedIds: string[];
}

class SubjectService {
  /**
   * Get all subjects for a tenant
   */
  async getSubjects(tenantSlug: string, token: string): Promise<{ data: SubjectListResponse[] | null; error: string | null }> {
    const response = await apiClient.get<SubjectListResponse[]>(`/${tenantSlug}/subjects`, token);
    return handleApiResponse(response);
  }

  /**
   * Get subject by ID
   */
  async getSubjectById(tenantSlug: string, subjectId: string, token: string): Promise<{ data: Subject | null; error: string | null }> {
    const response = await apiClient.get<Subject>(`/${tenantSlug}/subjects/${subjectId}`, token);
    return handleApiResponse(response);
  }

  /**
   * Create a new subject (uses bulk endpoint for single subject)
   */
  async createSubject(tenantSlug: string, subjectData: CreateSubjectRequest, token: string): Promise<{ data: Subject | null; error: string | null }> {
    // Use bulk endpoint for single subject creation
    const bulkRequest: BulkCreateSubjectsRequest = {
      Subjects: [subjectData]
    };

    const response = await apiClient.post<BulkCreateResponse>(`/${tenantSlug}/subjects/bulk`, bulkRequest, token);
    const result = handleApiResponse(response);

    if (result.error) {
      return { data: null, error: result.error };
    }

    if (result.data && result.data.SuccessfullyCreated > 0) {
      // For single subject creation, we need to fetch the created subject
      // Since bulk response only returns IDs, we'll return a success indicator
      return { data: null, error: null }; // Success but no subject data returned
    } else {
      const errorMessage = result.data?.Errors?.[0] || 'Failed to create subject';
      return { data: null, error: errorMessage };
    }
  }

  /**
   * Update an existing subject
   */
  async updateSubject(tenantSlug: string, subjectId: string, subjectData: UpdateSubjectRequest, token: string): Promise<{ data: Subject | null; error: string | null }> {
    const response = await apiClient.put<Subject>(`/${tenantSlug}/subjects/${subjectId}`, subjectData, token);
    return handleApiResponse(response);
  }

  /**
   * Delete a subject
   */
  async deleteSubject(tenantSlug: string, subjectId: string, token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.delete(`/${tenantSlug}/subjects/${subjectId}`, token);
    return handleApiResponse(response);
  }

  /**
   * Bulk create subjects
   */
  async bulkCreateSubjects(tenantSlug: string, request: BulkCreateSubjectsRequest, token: string): Promise<{ data: BulkCreateResponse | null; error: string | null }> {
    const response = await apiClient.post<BulkCreateResponse>(`/${tenantSlug}/subjects/bulk`, request, token);
    return handleApiResponse(response);
  }

  /**
   * Parse CSV data into subjects array
   */
  parseCSV(csvData: string): CreateSubjectRequest[] {
    const lines = csvData.trim().split('\n');
    const headers = lines[0].split(',').map(h => h.trim());

    const subjects: CreateSubjectRequest[] = [];

    for (let i = 1; i < lines.length; i++) {
      // Handle CSV parsing with quoted JSON arrays
      const line = lines[i];
      const values: string[] = [];
      let current = '';
      let inQuotes = false;

      for (let j = 0; j < line.length; j++) {
        const char = line[j];
        if (char === '"') {
          inQuotes = !inQuotes;
          current += char; // Include quotes in the current value
        } else if (char === ',' && !inQuotes) {
          values.push(current.trim());
          current = '';
        } else {
          current += char;
        }
      }
      values.push(current.trim());

      if (values.length >= headers.length) {
        // Parse RelatedEmployeeIds JSON array (backward compatibility)
        let relatedEmployeeIds: string[] | undefined;
        const relatedEmployeeIdsIndex = headers.indexOf('relatedEmployeeIds');
        if (relatedEmployeeIdsIndex !== -1 && values[relatedEmployeeIdsIndex]) {
          try {
            const jsonStr = values[relatedEmployeeIdsIndex].replace(/^"|"$/g, ''); // Remove outer quotes
            relatedEmployeeIds = JSON.parse(jsonStr);
          } catch (error) {
            console.warn('Failed to parse RelatedEmployeeIds JSON:', values[relatedEmployeeIdsIndex]);
            relatedEmployeeIds = undefined;
          }
        }

        // Parse EvaluatorRelationships JSON array (enhanced format)
        let evaluatorRelationships: EvaluatorRelationship[] | undefined;
        const evaluatorRelationshipsIndex = headers.indexOf('evaluatorRelationships');
        if (evaluatorRelationshipsIndex !== -1 && values[evaluatorRelationshipsIndex]) {
          try {
            const jsonStr = values[evaluatorRelationshipsIndex].replace(/^"|"$/g, ''); // Remove outer quotes
            evaluatorRelationships = JSON.parse(jsonStr);
          } catch (error) {
            console.warn('Failed to parse EvaluatorRelationships JSON:', values[evaluatorRelationshipsIndex]);
            evaluatorRelationships = undefined;
          }
        }

        const subject: CreateSubjectRequest = {
          FirstName: values[headers.indexOf('firstName')] || '',
          LastName: values[headers.indexOf('lastName')] || '',
          Email: values[headers.indexOf('email')] || '',
          EmployeeId: values[headers.indexOf('employeeId')] || '',
          CompanyName: values[headers.indexOf('companyName')] || '',
          Gender: values[headers.indexOf('gender')] || '',
          BusinessUnit: values[headers.indexOf('businessUnit')] || '',
          Grade: values[headers.indexOf('grade')] || '',
          Designation: values[headers.indexOf('designation')] || '',
          Tenure: parseInt(values[headers.indexOf('tenure')]) || undefined,
          Location: values[headers.indexOf('location')] || '',
          Metadata1: values[headers.indexOf('metadata1')] || '',
          Metadata2: values[headers.indexOf('metadata2')] || '',
          RelatedEmployeeIds: relatedEmployeeIds,
          EvaluatorRelationships: evaluatorRelationships,
        };

        if (subject.FirstName && subject.LastName && subject.Email && subject.EmployeeId) {
          subjects.push(subject);
        }
      }
    }

    return subjects;
  }

  /**
   * Generate CSV template
   */
  generateCSVTemplate(): string {
    return 'firstName,lastName,email,employeeId,companyName,gender,businessUnit,grade,designation,tenure,location,metadata1,metadata2,evaluatorRelationships\nJohn,Doe,john.doe@example.com,SUB001,Acme Corp,Male,Engineering,Senior,Software Engineer,5,New York,Team Lead,Full Stack,"[{""EvaluatorId"":""EVL001"",""Relationship"":""manager""},{""EvaluatorId"":""EVL002"",""Relationship"":""peer""}]"';
  }

  /**
   * Validate subject data
   */
  validateSubject(subject: CreateSubjectRequest): string[] {
    const errors: string[] = [];

    if (!subject.FirstName?.trim()) {
      errors.push('First name is required');
    }

    if (!subject.LastName?.trim()) {
      errors.push('Last name is required');
    }

    if (!subject.Email?.trim()) {
      errors.push('Email is required');
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(subject.Email)) {
      errors.push('Invalid email format');
    }

    if (!subject.EmployeeId?.trim()) {
      errors.push('Employee ID is required');
    } else if (subject.EmployeeId.length > 50) {
      errors.push('Employee ID must be 50 characters or less');
    }

    return errors;
  }

  /**
   * Validate evaluator EmployeeIds for relationship creation
   */
  async validateEvaluatorIds(tenantSlug: string, employeeIds: string[], token: string): Promise<string[]> {
    try {
      const response = await apiClient.post(`/${tenantSlug}/subjects/validate-evaluator-ids`, employeeIds, token);
      console.log('Raw API response:', response);

      // Handle different response formats based on status code
      if (response.status === 200) {
        // Single valid ID - response.data contains evaluator info
        console.log('Single ID validation - returning first ID');
        return [employeeIds[0]];
      } else if (response.status === 207) {
        // Multiple IDs - response.data contains ValidationResponse
        const apiResponse = response.data as any;
        console.log('Multi-status response received:', apiResponse);

        // Extract valid Employee IDs from the response
        const validEmployeeIds: string[] = [];

        if (apiResponse && apiResponse.Results && Array.isArray(apiResponse.Results)) {
          console.log(`Processing ${apiResponse.Results.length} results...`);

          for (let i = 0; i < apiResponse.Results.length; i++) {
            const result = apiResponse.Results[i];
            console.log(`Result ${i}:`, {
              EmployeeId: result.EmployeeId,
              IsValid: result.IsValid,
              Message: result.Message
            });

            // Check if this result is valid
            if (result.IsValid === true) {
              console.log(`✓ ${result.EmployeeId} is valid - adding to array`);
              validEmployeeIds.push(result.EmployeeId);
            } else {
              console.log(`✗ ${result.EmployeeId} is invalid - skipping`);
            }
          }
        } else {
          console.error('Invalid response structure:', apiResponse);
        }

        console.log('Final valid Employee IDs array:', validEmployeeIds);
        return validEmployeeIds;
      } else {
        // 404 or other error - no valid IDs
        console.log(`Unexpected status code: ${response.status}`);
        return [];
      }
    } catch (error: any) {
      // Handle 404 Not Found for single invalid ID
      if (error.response?.status === 404) {
        console.log('404 error - no valid IDs');
        return [];
      }
      console.error('Error validating evaluator EmployeeIds:', error);
      return [];
    }
  }
}

// Create and export a singleton instance
export const subjectService = new SubjectService();

// Export the class for testing or custom instances
export { SubjectService };
