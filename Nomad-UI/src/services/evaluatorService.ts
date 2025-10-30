/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, handleApiResponse } from './api';

// Types for Evaluator API
export interface Evaluator {
  Id: string;
  EmployeeId: string; // Guid FK to Employee table
  FirstName: string; // Computed from Employee
  LastName: string; // Computed from Employee
  FullName: string; // Computed from Employee
  Email: string; // Computed from Employee
  EvaluatorEmail: string; // Computed from Employee
  EmployeeIdString: string; // User-defined EmployeeId like "EMP001"
  CompanyName?: string; // Computed from Employee
  Designation?: string; // Computed from Employee
  Department?: string; // Computed from Employee
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  AssignedSubjectIds?: string[];
}

// List response has fewer fields than full evaluator response
export interface EvaluatorListResponse {
  Id: string;
  EmployeeId: string; // Guid FK to Employee table
  FirstName: string; // Computed from Employee
  LastName: string; // Computed from Employee
  FullName: string; // Computed from Employee
  Email: string; // Computed from Employee
  EvaluatorEmail: string; // Computed from Employee
  EmployeeIdString: string; // User-defined EmployeeId like "EMP001"
  CompanyName?: string; // Computed from Employee
  Designation?: string; // Computed from Employee
  Location?: string; // Computed from Employee
  IsActive: boolean;
  CreatedAt: string;
  LastLoginAt?: string;
  TenantId: string;
  SubjectCount: number;
}

export interface SubjectRelationship {
  SubjectEmployeeId: string; // EmployeeId (NOT GUID) of the subject (e.g., "SUB001")
  Relationship: string;
}

export interface CreateEvaluatorRequest {
  EmployeeId: string; // User-defined EmployeeId like "EMP001" (NOT the Guid)
  SubjectRelationships?: SubjectRelationship[];
}

export interface UpdateEvaluatorRequest {
  EmployeeId: string; // User-defined EmployeeId like "EMP001" (NOT the Guid)
}

export interface BulkCreateEvaluatorsRequest {
  Evaluators: CreateEvaluatorRequest[];
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

export interface BulkCreateResponse {
  TotalRequested: number;
  SuccessfullyCreated: number;
  UpdatedCount: number;
  Failed: number;
  Errors: string[];
  CreatedIds: string[];
}

class EvaluatorService {
  /**
   * Get all evaluators for a tenant
   */
  async getEvaluators(tenantSlug: string, token: string): Promise<{ data: EvaluatorListResponse[] | null; error: string | null }> {
    const response = await apiClient.get<EvaluatorListResponse[]>(`/${tenantSlug}/evaluators`, token);
    return handleApiResponse(response);
  }

  /**
   * Get evaluator by ID
   */
  async getEvaluatorById(tenantSlug: string, evaluatorId: string, token: string): Promise<{ data: Evaluator | null; error: string | null }> {
    const response = await apiClient.get<Evaluator>(`/${tenantSlug}/evaluators/${evaluatorId}`, token);
    return handleApiResponse(response);
  }

  /**
   * Create a new evaluator (uses bulk endpoint for single evaluator)
   */
  async createEvaluator(tenantSlug: string, evaluatorData: CreateEvaluatorRequest, token: string): Promise<{ data: Evaluator | null; error: string | null }> {
    // Use bulk endpoint for single evaluator creation
    const bulkRequest: BulkCreateEvaluatorsRequest = {
      Evaluators: [evaluatorData]
    };

    const response = await apiClient.post<BulkCreateResponse>(`/${tenantSlug}/evaluators/bulk`, bulkRequest, token);
    const result = handleApiResponse(response);

    if (result.error) {
      return { data: null, error: result.error };
    }

    if (result.data && result.data.SuccessfullyCreated > 0) {
      // For single evaluator creation, we need to fetch the created evaluator
      // Since bulk response only returns IDs, we'll return a success indicator
      return { data: null, error: null }; // Success but no evaluator data returned
    } else {
      const errorMessage = result.data?.Errors?.[0] || 'Failed to create evaluator';
      return { data: null, error: errorMessage };
    }
  }

  /**
   * Update an existing evaluator
   */
  async updateEvaluator(tenantSlug: string, evaluatorId: string, evaluatorData: UpdateEvaluatorRequest, token: string): Promise<{ data: Evaluator | null; error: string | null }> {
    const response = await apiClient.put<Evaluator>(`/${tenantSlug}/evaluators/${evaluatorId}`, evaluatorData, token);
    return handleApiResponse(response);
  }

  /**
   * Delete an evaluator
   */
  async deleteEvaluator(tenantSlug: string, evaluatorId: string, token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.delete(`/${tenantSlug}/evaluators/${evaluatorId}`, token);
    return handleApiResponse(response);
  }

  /**
   * Bulk create evaluators with relationships
   */
  async bulkCreateEvaluators(
    tenantSlug: string,
    request: BulkCreateEvaluatorsRequest,
    token: string
  ): Promise<{ data: BulkCreateResponse | null; error: string | null }> {
    try {
      console.log('🔵 [SERVICE] bulkCreateEvaluators called');
      console.log('🔵 [SERVICE] tenantSlug:', tenantSlug);
      console.log('🔵 [SERVICE] request.Evaluators.length:', request.Evaluators.length);
      console.log('🔵 [SERVICE] token exists:', !!token);

      const response = await apiClient.post<BulkCreateResponse>(
        `/${tenantSlug}/evaluators/bulk`,
        request,
        token
      );

      console.log('✅ [SERVICE] API response status:', response.status);
      console.log('✅ [SERVICE] API response data:', response.data);

      return handleApiResponse(response);
    } catch (error: any) {
      console.error('❌ [SERVICE] Error in bulkCreateEvaluators:', error);
      return { data: null, error: error.message || 'Failed to bulk create evaluators' };
    }
  }

  /**
   * Parse CSV data into evaluators array with comprehensive validation
   */
  parseCSV(csvData: string): { evaluators: CreateEvaluatorRequest[]; errors: string[] } {
    const errors: string[] = [];
    const evaluators: CreateEvaluatorRequest[] = [];

    try {
      const lines = csvData.trim().split('\n');

      if (lines.length === 0) {
        errors.push('CSV file is empty');
        return { evaluators, errors };
      }

      // Parse headers (case-insensitive)
      const headers = lines[0].split(',').map(h => h.trim().toLowerCase());

      // Validate required headers
      const requiredHeaders = ['firstname', 'lastname', 'evaluatoremail', 'employeeid'];
      const missingHeaders = requiredHeaders.filter(h => !headers.includes(h));

      if (missingHeaders.length > 0) {
        errors.push(`Missing required columns: ${missingHeaders.join(', ')}`);
        return { evaluators, errors };
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

          // Parse SubjectRelationships JSON array
          let subjectRelationships: SubjectRelationship[] | undefined;
          const relationshipsStr = getField('subjectrelationships');

          if (relationshipsStr) {
            try {
              // Remove outer quotes if present and unescape doubled quotes
              const jsonStr = relationshipsStr
                .replace(/^"|"$/g, '')
                .replace(/""/g, '"');

              const parsedData = JSON.parse(jsonStr);

              if (!Array.isArray(parsedData)) {
                errors.push(`Row ${lineNumber}: SubjectRelationships must be a JSON array`);
              } else {
                // Normalize property names (case-insensitive)
                subjectRelationships = parsedData.map((item: any, idx: number) => {
                  const lowerMap: Record<string, any> = {};
                  Object.keys(item).forEach(k => { lowerMap[k.toLowerCase()] = item[k]; });

                  const subjectEmployeeId = lowerMap['subjectemployeeid'] ?? lowerMap['subjectid'] ?? '';
                  const relationship = lowerMap['relationship'] ?? '';

                  if (!subjectEmployeeId) {
                    errors.push(`Row ${lineNumber}: SubjectRelationships[${idx}] missing SubjectEmployeeId`);
                  }
                  if (!relationship) {
                    errors.push(`Row ${lineNumber}: SubjectRelationships[${idx}] missing Relationship`);
                  }

                  // Validate self-evaluation: if relationship is "Self", SubjectEmployeeId must equal EvaluatorEmployeeId
                  if (relationship.toLowerCase() === 'self' && subjectEmployeeId.toLowerCase() !== employeeId.toLowerCase()) {
                    errors.push(`Row ${lineNumber}: SubjectRelationships[${idx}] - Relationship "Self" requires SubjectEmployeeId and EvaluatorEmployeeId to match (EvaluatorEmployeeId: ${employeeId}, SubjectEmployeeId: ${subjectEmployeeId})`);
                  }

                  return {
                    SubjectEmployeeId: String(subjectEmployeeId),
                    Relationship: String(relationship)
                  };
                });
              }
            } catch (parseError: any) {
              errors.push(`Row ${lineNumber}: Failed to parse SubjectRelationships JSON - ${parseError.message}`);
              subjectRelationships = undefined;
            }
          }

          // Create evaluator request object
          const evaluator: CreateEvaluatorRequest = {
            EmployeeId: employeeId,
            SubjectRelationships: subjectRelationships
          };

          evaluators.push(evaluator);
        } catch (rowError: any) {
          errors.push(`Row ${lineNumber}: ${rowError.message}`);
        }
      }

      if (evaluators.length === 0 && errors.length === 0) {
        errors.push('No valid data rows found in CSV');
      }

    } catch (error: any) {
      errors.push(`CSV parsing error: ${error.message}`);
    }

    return { evaluators, errors };
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
      'evaluatorEmail',
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
      'subjectRelationships'
    ];

    const sampleRow = [
      'Jane',
      'Smith',
      'jane.smith@example.com',
      'EVL001',
      'Acme Corp',
      'Female',
      'HR',
      'Manager',
      'HR Manager',
      '3',
      'Boston',
      'Team Lead',
      'People Operations',
      '"[{""SubjectEmployeeId"":""SUB001"",""Relationship"":""direct-report""},{""SubjectEmployeeId"":""SUB002"",""Relationship"":""direct-report""}]"'
    ];

    return `${headers.join(',')}\n${sampleRow.join(',')}`;
  }






  /**
   * Validate evaluator data
   */
  validateEvaluator(evaluator: CreateEvaluatorRequest): string[] {
    const errors: string[] = [];

    if (!evaluator.EmployeeId?.trim()) {
      errors.push('Employee ID is required');
    } else if (evaluator.EmployeeId.length > 50) {
      errors.push('Employee ID must be 50 characters or less');
    }

    return errors;
  }



  /**
   * Get existing subject relationships for an evaluator
   */
  async getEvaluatorSubjects(tenantSlug: string, evaluatorId: string, token: string): Promise<{ data: any[] | null; error: string | null }> {
    try {
      const response = await apiClient.get<any[]>(`/${tenantSlug}/subject-evaluators/evaluators/${evaluatorId}/subjects`, token);
      return handleApiResponse(response);
    } catch (error: any) {
      console.error('Error fetching evaluator subjects:', error);
      return { data: null, error: error.message || 'Failed to fetch evaluator subjects' };
    }
  }

  /**
   * Assign subjects to an evaluator
   */
  async assignSubjectsToEvaluator(
    tenantSlug: string,
    evaluatorId: string,
    subjects: { SubjectEmployeeId: string; Relationship: string }[],
    token: string
  ): Promise<{ success: boolean; data?: any; error: string | null }> {
    try {
      const payload = {
        Subjects: subjects.map(s => ({
          SubjectId: s.SubjectEmployeeId,
          Relationship: s.Relationship
        }))
      };

      console.log('assignSubjectsToEvaluator payload:', payload);

      const response = await apiClient.post(
        `/${tenantSlug}/subject-evaluators/evaluators/${evaluatorId}/subjects`,
        payload,
        token
      );

      if (response.status === 200 && response.data) {
        return { success: true, data: response.data, error: null };
      }

      return { success: false, error: 'Failed to assign subjects' };
    } catch (error: unknown) {
      console.error('Error assigning subjects to evaluator:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to assign subjects';
      return { success: false, error: errorMessage };
    }
  }

  /**
   * Update a subject relationship for an evaluator
   */
  async updateEvaluatorSubject(
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
      console.error('Error updating evaluator subject:', error);
      return { success: false, error: error.message || 'Failed to update relationship' };
    }
  }

  /**
   * Remove a subject relationship from an evaluator
   */
  async removeEvaluatorSubject(tenantSlug: string, subjectId: string, evaluatorId: string, token: string): Promise<{ success: boolean; error: string | null }> {
    try {
      const response = await apiClient.delete(`/${tenantSlug}/subject-evaluators/subjects/${subjectId}/evaluators/${evaluatorId}`, token);
      if (response.status === 204) {
        return { success: true, error: null };
      }
      return { success: false, error: 'Failed to remove relationship' };
    } catch (error: any) {
      console.error('Error removing evaluator subject:', error);
      return { success: false, error: error.message || 'Failed to remove relationship' };
    }
  }
}

// Create and export a singleton instance
export const evaluatorService = new EvaluatorService();

// Export the class for testing or custom instances
export { EvaluatorService };
