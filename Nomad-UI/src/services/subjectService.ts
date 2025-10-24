/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, handleApiResponse } from './api';

// Types for Subject API
export interface Subject {
  Id: string;
  EmployeeId: string; // Guid FK to Employee table
  FirstName: string; // Computed from Employee
  LastName: string; // Computed from Employee
  FullName: string; // Computed from Employee
  Email: string; // Computed from Employee
  EmployeeIdString: string; // User-defined EmployeeId like "EMP001"
  CompanyName?: string; // Computed from Employee
  Designation?: string; // Computed from Employee
  Department?: string; // Computed from Employee
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  AssignedEvaluatorIds?: string[];
}

// List response has fewer fields than full subject response
export interface SubjectListResponse {
  Id: string;
  EmployeeId: string; // Guid FK to Employee table
  FirstName: string; // Computed from Employee
  LastName: string; // Computed from Employee
  FullName: string; // Computed from Employee
  Email: string; // Computed from Employee
  EmployeeIdString: string; // User-defined EmployeeId like "EMP001"
  CompanyName?: string; // Computed from Employee
  Designation?: string; // Computed from Employee
  Location?: string; // Computed from Employee
  IsActive: boolean;
  CreatedAt: string;
  LastLoginAt?: string;
  TenantId: string;
  EvaluatorCount: number;
}

export interface EvaluatorRelationship {
  EvaluatorEmployeeId: string; // EmployeeId (NOT GUID) of the evaluator (e.g., "EMP0097")
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
  EmployeeId: string; // User-defined EmployeeId like "EMP001" (NOT the Guid)
  EvaluatorRelationships?: EvaluatorRelationship[];
}

export interface UpdateSubjectRequest {
  EmployeeId: string; // User-defined EmployeeId like "EMP001" (NOT the Guid)
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
   * Bulk create subjects with relationships
   */
  async bulkCreateSubjects(
    tenantSlug: string,
    request: BulkCreateSubjectsRequest,
    token: string
  ): Promise<{ data: BulkCreateResponse | null; error: string | null }> {
    try {
      console.log('🔵 [SERVICE] bulkCreateSubjects called');
      console.log('🔵 [SERVICE] tenantSlug:', tenantSlug);
      console.log('🔵 [SERVICE] request.Subjects.length:', request.Subjects.length);
      console.log('🔵 [SERVICE] token exists:', !!token);

      const response = await apiClient.post<BulkCreateResponse>(
        `/${tenantSlug}/subjects/bulk`,
        request,
        token
      );

      console.log('✅ [SERVICE] API response status:', response.status);
      console.log('✅ [SERVICE] API response data:', response.data);

      return handleApiResponse(response);
    } catch (error: any) {
      console.error('❌ [SERVICE] Error in bulkCreateSubjects:', error);
      return { data: null, error: error.message || 'Failed to bulk create subjects' };
    }
  }

  /**
   * Parse CSV data into subjects array with comprehensive validation
   */
  parseCSV(csvData: string): { subjects: CreateSubjectRequest[]; errors: string[] } {
    const errors: string[] = [];
    const subjects: CreateSubjectRequest[] = [];

    try {
      const lines = csvData.trim().split('\n');

      if (lines.length === 0) {
        errors.push('CSV file is empty');
        return { subjects, errors };
      }

      // Parse headers (case-insensitive)
      const headers = lines[0].split(',').map(h => h.trim().toLowerCase());

      // Validate required headers
      const requiredHeaders = ['firstname', 'lastname', 'email', 'employeeid'];
      const missingHeaders = requiredHeaders.filter(h => !headers.includes(h));

      if (missingHeaders.length > 0) {
        errors.push(`Missing required columns: ${missingHeaders.join(', ')}`);
        return { subjects, errors };
      }

      // Track duplicate EmployeeIds within the CSV
      const seenEmployeeIds = new Set<string>();

      // Parse data rows
      for (let i = 1; i < lines.length; i++) {
        const lineNumber = i + 1;

        // Skip empty lines
        if (!lines[i].trim()) {
          continue;
        }

        try {
          // Parse CSV line handling quoted fields with commas and JSON
          const values = this.parseCSVLine(lines[i]);

          if (values.length < headers.length) {
            errors.push(`Row ${lineNumber}: Insufficient columns (expected ${headers.length}, got ${values.length})`);
            continue;
          }

          // Extract field values
          const getField = (fieldName: string): string => {
            const index = headers.indexOf(fieldName.toLowerCase());
            return index !== -1 ? values[index].trim() : '';
          };

          const employeeId = getField('employeeid');

          // Validate required fields
          if (!employeeId) {
            errors.push(`Row ${lineNumber}: EmployeeId is required`);
            continue;
          }

          // Check for duplicate EmployeeId within CSV
          if (seenEmployeeIds.has(employeeId.toLowerCase())) {
            errors.push(`Row ${lineNumber}: Duplicate EmployeeId "${employeeId}" found in CSV (only first occurrence will be processed)`);
            continue;
          }
          seenEmployeeIds.add(employeeId.toLowerCase());

          // Parse EvaluatorRelationships JSON array
          let evaluatorRelationships: EvaluatorRelationship[] | undefined;
          const relationshipsStr = getField('evaluatorrelationships');

          if (relationshipsStr) {
            try {
              // Remove outer quotes if present and unescape doubled quotes
              const jsonStr = relationshipsStr
                .replace(/^"|"$/g, '')
                .replace(/""/g, '"');

              const parsedData = JSON.parse(jsonStr);

              if (!Array.isArray(parsedData)) {
                errors.push(`Row ${lineNumber}: EvaluatorRelationships must be a JSON array`);
              } else {
                // Normalize property names (case-insensitive)
                evaluatorRelationships = parsedData.map((item: any, idx: number) => {
                  const lowerMap: Record<string, any> = {};
                  Object.keys(item).forEach(k => { lowerMap[k.toLowerCase()] = item[k]; });

                  const evaluatorEmployeeId = lowerMap['evaluatoremployeeid'] ?? lowerMap['evaluatorid'] ?? '';
                  const relationship = lowerMap['relationship'] ?? '';

                  if (!evaluatorEmployeeId) {
                    errors.push(`Row ${lineNumber}: EvaluatorRelationships[${idx}] missing EvaluatorEmployeeId`);
                  }
                  if (!relationship) {
                    errors.push(`Row ${lineNumber}: EvaluatorRelationships[${idx}] missing Relationship`);
                  }

                  return {
                    EvaluatorEmployeeId: String(evaluatorEmployeeId),
                    Relationship: String(relationship)
                  };
                });
              }
            } catch (parseError: any) {
              errors.push(`Row ${lineNumber}: Failed to parse EvaluatorRelationships JSON - ${parseError.message}`);
              evaluatorRelationships = undefined;
            }
          }

          // Create subject request object
          const subject: CreateSubjectRequest = {
            EmployeeId: employeeId,
            EvaluatorRelationships: evaluatorRelationships
          };

          subjects.push(subject);
        } catch (rowError: any) {
          errors.push(`Row ${lineNumber}: ${rowError.message}`);
        }
      }

      if (subjects.length === 0 && errors.length === 0) {
        errors.push('No valid data rows found in CSV');
      }

    } catch (error: any) {
      errors.push(`CSV parsing error: ${error.message}`);
    }

    return { subjects, errors };
  }

  /**
   * Parse a single CSV line handling quoted fields with commas and escaped quotes
   */
  private parseCSVLine(line: string): string[] {
    const values: string[] = [];
    let current = '';
    let inQuotes = false;

    for (let i = 0; i < line.length; i++) {
      const char = line[i];
      const nextChar = i < line.length - 1 ? line[i + 1] : '';

      if (char === '"') {
        if (inQuotes && nextChar === '"') {
          // Escaped quote (doubled quotes)
          current += '"';
          i++; // Skip next quote
        } else {
          // Toggle quote state
          inQuotes = !inQuotes;
        }
      } else if (char === ',' && !inQuotes) {
        // Field separator
        values.push(current);
        current = '';
      } else {
        current += char;
      }
    }

    // Add last field
    values.push(current);

    return values;
  }

  /**
   * Generate CSV template with sample data
   */
  generateCSVTemplate(): string {
    const headers = [
      'firstName',
      'lastName',
      'email',
      'employeeId',
      'companyName',
      'gender',
      'businessUnit',
      'grade',
      'designation',
      'tenure',
      'location',
      'metadata1',
      'metadata2',
      'evaluatorRelationships'
    ];

    const sampleRow = [
      'John',
      'Doe',
      'john.doe@example.com',
      'SUB001',
      'Acme Corp',
      'Male',
      'Engineering',
      'Senior',
      'Software Engineer',
      '5',
      'New York',
      'Team Lead',
      'Full Stack',
      '"[{""EvaluatorEmployeeId"":""EMP001"",""Relationship"":""manager""},{""EvaluatorEmployeeId"":""EMP002"",""Relationship"":""peer""}]"'
    ];

    return `${headers.join(',')}\n${sampleRow.join(',')}`;
  }






  /**
   * Validate subject data
   */
  validateSubject(subject: CreateSubjectRequest): string[] {
    const errors: string[] = [];

    if (!subject.EmployeeId?.trim()) {
      errors.push('Employee ID is required');
    } else if (subject.EmployeeId.length > 50) {
      errors.push('Employee ID must be 50 characters or less');
    }

    return errors;
  }



  /**
   * Get existing evaluator relationships for a subject
   */
  async getSubjectEvaluators(tenantSlug: string, subjectId: string, token: string): Promise<{ data: any[] | null; error: string | null }> {
    try {
      const response = await apiClient.get<any[]>(`/${tenantSlug}/subject-evaluators/subjects/${subjectId}/evaluators`, token);
      return handleApiResponse(response);
    } catch (error: any) {
      console.error('Error fetching subject evaluators:', error);
      return { data: null, error: error.message || 'Failed to fetch subject evaluators' };
    }
  }

  /**
   * Assign evaluators to a subject
   */
  async assignEvaluatorsToSubject(
    tenantSlug: string,
    subjectId: string,
    evaluators: { EvaluatorId: string; Relationship: string }[],
    token: string
  ): Promise<{ success: boolean; data?: any; error: string | null }> {
    try {
      const payload = {
        Evaluators: evaluators.map(e => ({
          EvaluatorId: e.EvaluatorId,
          Relationship: e.Relationship
        }))
      };

      console.log('assignEvaluatorsToSubject payload:', payload);

      const response = await apiClient.post(
        `/${tenantSlug}/subject-evaluators/subjects/${subjectId}/evaluators`,
        payload,
        token
      );

      if (response.status === 200 && response.data) {
        return { success: true, data: response.data, error: null };
      }

      return { success: false, error: (response.data as any)?.Message || 'Failed to assign evaluators' };
    } catch (error: any) {
      console.error('Error assigning evaluators to subject:', error);
      return { success: false, error: error.message || 'Failed to assign evaluators' };
    }
  }

  /**
   * Update an evaluator relationship for a subject
   */
  async updateSubjectEvaluator(
    tenantSlug: string,
    subjectId: string,
    evaluatorId: string,
    relationship: string,
    token: string
  ): Promise<{ success: boolean; data?: any; error: string | null }> {
    try {
      const payload = { Relationship: relationship };

      const response = await apiClient.put(
        `/${tenantSlug}/subject-evaluators/subjects/${subjectId}/evaluators/${evaluatorId}`,
        payload,
        token
      );

      if (response.status === 200 && response.data) {
        return { success: true, data: response.data, error: null };
      }

      return { success: false, error: (response.data as any)?.Message || 'Failed to update relationship' };
    } catch (error: any) {
      console.error('Error updating subject evaluator:', error);
      return { success: false, error: error.message || 'Failed to update relationship' };
    }
  }

  /**
   * Remove an evaluator relationship from a subject
   */
  async removeSubjectEvaluator(tenantSlug: string, subjectId: string, evaluatorId: string, token: string): Promise<{ success: boolean; error: string | null }> {
    try {
      const response = await apiClient.delete(`/${tenantSlug}/subject-evaluators/subjects/${subjectId}/evaluators/${evaluatorId}`, token);
      if (response.status === 204) {
        return { success: true, error: null };
      }
      return { success: false, error: 'Failed to remove relationship' };
    } catch (error: any) {
      console.error('Error removing subject evaluator:', error);
      return { success: false, error: error.message || 'Failed to remove relationship' };
    }
  }
}

// Create and export a singleton instance
export const subjectService = new SubjectService();

// Export the class for testing or custom instances
export { SubjectService };
