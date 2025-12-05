/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, handleApiResponse } from './api';

export interface AdditionalAttribute {
  Key: string;
  Value: string;
}

export interface Employee {
  Id: string;
  FirstName: string;
  LastName: string;
  FullName: string;
  Email: string;
  Number?: string;
  EmployeeId: string;
  CompanyName?: string;
  Designation?: string;
  Department?: string;
  Tenure?: number;
  Grade?: string;
  Gender?: string;
  ManagerId?: string;
  MoreInfo?: AdditionalAttribute[];
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  SubjectId?: string;
  EvaluatorId?: string;
}

export interface EmployeeListResponse {
  Id: string;
  FirstName: string;
  LastName: string;
  FullName: string;
  Email: string;
  Number?: string;
  EmployeeId: string;
  CompanyName?: string;
  Designation?: string;
  Department?: string;
  IsActive: boolean;
}

export interface CreateEmployeeRequest {
  FirstName: string;
  LastName: string;
  Email: string;
  Number?: string;
  EmployeeId: string;
  CompanyName?: string;
  Designation?: string;
  Department?: string;
  Tenure?: number;
  Grade?: string;
  Gender?: string;
  ManagerId?: string;
  MoreInfo?: AdditionalAttribute[];
  SubjectId?: string;
  EvaluatorId?: string;
}

export interface UpdateEmployeeRequest {
  FirstName: string;
  LastName: string;
  Email: string;
  Number?: string;
  EmployeeId: string;
  CompanyName?: string;
  Designation?: string;
  Department?: string;
  Tenure?: number;
  Grade?: string;
  Gender?: string;
  ManagerId?: string;
  MoreInfo?: AdditionalAttribute[];
  SubjectId?: string;
  EvaluatorId?: string;
}

export interface BulkCreateEmployeesRequest {
  Employees: CreateEmployeeRequest[];
}

export interface BulkCreateResponse {
  TotalRequested: number;
  SuccessfullyCreated: number;
  UpdatedCount: number;
  Failed: number;
  Errors: string[];
  CreatedIds: string[];
}

class EmployeeService {
  /**
   * Get all employees with optional filtering
   */
  async getEmployees(
    tenantSlug: string,
    token: string,
    filters?: {
      name?: string;
      designation?: string;
      department?: string;
      email?: string;
    }
  ): Promise<{ data: EmployeeListResponse[] | null; error: string | null }> {
    try {
      const queryParams = new URLSearchParams();
      if (filters?.name) queryParams.append('name', filters.name);
      if (filters?.designation) queryParams.append('designation', filters.designation);
      if (filters?.department) queryParams.append('department', filters.department);
      if (filters?.email) queryParams.append('email', filters.email);

      const queryString = queryParams.toString();
      const url = `/${tenantSlug}/employees${queryString ? `?${queryString}` : ''}`;

      const response = await apiClient.get<EmployeeListResponse[]>(url, token);
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to fetch employees' };
    }
  }

  /**
   * Get a specific employee by ID
   */
  async getEmployeeById(
    tenantSlug: string,
    employeeId: string,
    token: string
  ): Promise<{ data: Employee | null; error: string | null }> {
    try {
      const response = await apiClient.get<Employee>(`/${tenantSlug}/employees/${employeeId}`, token);
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to fetch employee' };
    }
  }

  /**
   * Create a new employee (uses bulk endpoint for single employee)
   */
  async createEmployee(tenantSlug: string, employeeData: CreateEmployeeRequest, token: string): Promise<{ data: Employee | null; error: string | null }> {
    // Use bulk endpoint for single employee creation
    const bulkRequest: BulkCreateEmployeesRequest = {
      Employees: [employeeData]
    };

    const response = await apiClient.post<BulkCreateResponse>(`/${tenantSlug}/employees/bulk`, bulkRequest, token);
    const result = handleApiResponse(response);

    if (result.error) {
      return { data: null, error: result.error };
    }

    // Fetch the created employee
    if (result.data && result.data.CreatedIds.length > 0) {
      return this.getEmployeeById(tenantSlug, result.data.CreatedIds[0], token);
    }

    return { data: null, error: 'Employee creation failed' };
  }

  /**
   * Bulk create employees
   */
  async bulkCreateEmployees(
    tenantSlug: string,
    request: BulkCreateEmployeesRequest,
    token: string
  ): Promise<{ data: BulkCreateResponse | null; error: string | null }> {
    try {
      console.log('🔵 [SERVICE] bulkCreateEmployees called');
      console.log('🔵 [SERVICE] tenantSlug:', tenantSlug);
      console.log('🔵 [SERVICE] request.Employees.length:', request.Employees.length);
      console.log('🔵 [SERVICE] token exists:', !!token);

      const response = await apiClient.post<BulkCreateResponse>(
        `/${tenantSlug}/employees/bulk`,
        request,
        token
      );

      console.log('✅ [SERVICE] API response status:', response.status);
      console.log('✅ [SERVICE] API response data:', response.data);

      return handleApiResponse(response);
    } catch (error: any) {
      console.error('❌ [SERVICE] Error in bulkCreateEmployees:', error);
      return { data: null, error: error.message || 'Failed to bulk create employees' };
    }
  }

  /**
   * Update an employee
   */
  async updateEmployee(
    tenantSlug: string,
    employeeId: string,
    employeeData: UpdateEmployeeRequest,
    token: string
  ): Promise<{ data: Employee | null; error: string | null }> {
    try {
      const response = await apiClient.put<Employee>(
        `/${tenantSlug}/employees/${employeeId}`,
        employeeData,
        token
      );
      return handleApiResponse(response);
    } catch (error: any) {
      return { data: null, error: error.message || 'Failed to update employee' };
    }
  }

  /**
   * Delete an employee
   */
  async deleteEmployee(
    tenantSlug: string,
    employeeId: string,
    token: string
  ): Promise<{ success: boolean; error: string | null }> {
    try {
      const response = await apiClient.delete(`/${tenantSlug}/employees/${employeeId}`, token);
      
      if (response.status === 200) {
        return { success: true, error: null };
      }
      
      return { success: false, error: 'Failed to delete employee' };
    } catch (error: any) {
      return { success: false, error: error.message || 'Failed to delete employee' };
    }
  }

  /**
   * Parse CSV data into employees array with comprehensive validation
   */
  parseCSV(csvData: string): { employees: CreateEmployeeRequest[]; errors: string[] } {
    const errors: string[] = [];
    const employees: CreateEmployeeRequest[] = [];

    try {
      const lines = csvData.trim().split('\n');

      if (lines.length === 0) {
        errors.push('CSV file is empty');
        return { employees, errors };
      }

      // Parse headers (case-insensitive)
      const headers = lines[0].split(',').map(h => h.trim().toLowerCase());

      // Validate required headers
      const requiredHeaders = ['firstname', 'lastname', 'email', 'employeeid'];
      const missingHeaders = requiredHeaders.filter(h => !headers.includes(h));

      if (missingHeaders.length > 0) {
        errors.push(`Missing required columns: ${missingHeaders.join(', ')}`);
        return { employees, errors };
      }

      // Track duplicate EmployeeIds and Emails within the CSV
      const seenEmployeeIds = new Set<string>();
      const seenEmails = new Set<string>();

      // Process data rows
      for (let i = 1; i < lines.length; i++) {
        const lineNumber = i + 1;

        try {
          // Parse CSV line handling quoted fields with commas
          const values = this.parseCSVLine(lines[i]);

          // Extract field values (allow missing optional columns)
          const getField = (fieldName: string): string => {
            const index = headers.indexOf(fieldName.toLowerCase());
            return index !== -1 && index < values.length ? values[index].trim() : '';
          };

          const employeeId = getField('employeeid');
          const firstName = getField('firstname');
          const lastName = getField('lastname');
          const email = getField('email');
          const number = getField('number');

          // Validate required fields
          if (!firstName) {
            errors.push(`Row ${lineNumber} skipped${employeeId ? ` (ID: ${employeeId})` : ''}: First name is missing`);
            continue;
          }
          if (!lastName) {
            errors.push(`Row ${lineNumber} skipped${employeeId ? ` (ID: ${employeeId})` : ''}: Last name is missing`);
            continue;
          }
          if (!email) {
            errors.push(`Row ${lineNumber} skipped${employeeId ? ` (ID: ${employeeId})` : ''}: Email address is missing`);
            continue;
          }
          if (!employeeId) {
            errors.push(`Row ${lineNumber} skipped: Employee ID is missing`);
            continue;
          }

          // Validate minimum character length for names (at least 2 characters)
          if (firstName.length < 2) {
            errors.push(`Row ${lineNumber} skipped (ID: ${employeeId}): First name must be at least 2 characters long`);
            continue;
          }
          if (lastName.length < 2) {
            errors.push(`Row ${lineNumber} skipped (ID: ${employeeId}): Last name must be at least 2 characters long`);
            continue;
          }

          // Validate maximum character length for names (reasonable limit)
          if (firstName.length > 50) {
            errors.push(`Row ${lineNumber} skipped (ID: ${employeeId}): First name is too long (maximum 50 characters)`);
            continue;
          }
          if (lastName.length > 50) {
            errors.push(`Row ${lineNumber} skipped (ID: ${employeeId}): Last name is too long (maximum 50 characters)`);
            continue;
          }

          // Check for duplicate EmployeeId within CSV
          if (seenEmployeeIds.has(employeeId.toLowerCase())) {
            errors.push(`Row ${lineNumber} skipped (ID: ${employeeId}): This employee ID appears multiple times in the file`);
            continue;
          }
          seenEmployeeIds.add(employeeId.toLowerCase());

          // Validate email format (more robust regex)
          const emailRegex = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
          if (!emailRegex.test(email)) {
            errors.push(`Row ${lineNumber} skipped (ID: ${employeeId}): Email address "${email}" is not valid`);
            continue;
          }

          // Check for duplicate Email within CSV
          if (seenEmails.has(email.toLowerCase())) {
            errors.push(`Row ${lineNumber} skipped (ID: ${employeeId}): Email address "${email}" appears multiple times in the file`);
            continue;
          }
          seenEmails.add(email.toLowerCase());

          // Validate phone number format if provided
          if (number) {
            // Remove common separators for validation
            const cleanNumber = number.replace(/[\s\-\(\)\.]/g, '');

            // Check if it contains only digits and optional + at the start
            const phoneRegex = /^\+?[0-9]{7,15}$/;
            if (!phoneRegex.test(cleanNumber)) {
              errors.push(`Row ${lineNumber} skipped (ID: ${employeeId}): Phone number "${number}" is not valid (must be 7-15 digits)`);
              continue;
            }
          }

          // Parse tenure
          let tenure: number | undefined;
          const tenureStr = getField('tenure');
          if (tenureStr) {
            tenure = parseInt(tenureStr, 10);
            if (isNaN(tenure) || tenure < 0 || tenure > 100) {
              errors.push(`Row ${lineNumber} skipped (ID: ${employeeId}): Tenure value "${tenureStr}" is not valid (must be between 0 and 100)`);
              continue;
            }
          }

          // Parse additional attributes from extra columns
          const standardColumns = [
            'firstname', 'lastname', 'email', 'number', 'employeeid',
            'companyname', 'designation', 'department', 'tenure',
            'grade', 'gender', 'managerid', 'subjectid', 'evaluatorid'
          ];

          const moreInfo: AdditionalAttribute[] = [];
          headers.forEach((header, index) => {
            if (!standardColumns.includes(header) && values[index] && values[index].trim()) {
              moreInfo.push({
                Key: header,
                Value: values[index].trim()
              });
            }
          });

          // Create employee request object
          const employee: CreateEmployeeRequest = {
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            Number: number || undefined,
            EmployeeId: employeeId,
            CompanyName: getField('companyname') || undefined,
            Designation: getField('designation') || undefined,
            Department: getField('department') || undefined,
            Tenure: tenure,
            Grade: getField('grade') || undefined,
            Gender: getField('gender') || undefined,
            ManagerId: getField('managerid') || undefined,
            MoreInfo: moreInfo.length > 0 ? moreInfo : undefined,
          };

          employees.push(employee);
        } catch (rowError: any) {
          errors.push(`Row ${lineNumber} skipped: ${rowError.message}`);
        }
      }

      if (employees.length === 0 && errors.length === 0) {
        errors.push('No employee data found in the CSV file');
      }

    } catch (error: any) {
      errors.push(`Unable to read CSV file: ${error.message}`);
    }

    return { employees, errors };
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
   * Note: Only firstName, lastName, email, and employeeId are REQUIRED
   * All other columns are OPTIONAL and can be omitted from the CSV
   * Any additional columns beyond the standard ones will be treated as custom attributes
   */
  generateCSVTemplate(): string {
    const headers = [
      'firstName',      // REQUIRED
      'lastName',       // REQUIRED
      'email',          // REQUIRED
      'employeeId',     // REQUIRED
      'number',         // Optional
      'companyName',    // Optional
      'designation',    // Optional
      'department',     // Optional
      'tenure',         // Optional
      'grade',          // Optional
      'gender',         // Optional
      'managerId',      // Optional
      // Add custom attribute columns here - they will be stored in MoreInfo
      'skills',         // Optional (custom attribute)
      'certifications', // Optional (custom attribute)
      'languages'       // Optional (custom attribute)
    ];

    const sampleRow = [
      'John',
      'Doe',
      'john.doe@example.com',
      'EMP001',
      '+1234567890',
      'Acme Corp',
      'Software Engineer',
      'Engineering',
      '5',
      'Senior',
      'Male',
      'MGR001',
      // Custom attributes (optional)
      'JavaScript, React, Node.js',
      'AWS Certified',
      'English, Spanish'
    ];

    return `${headers.join(',')}\n${sampleRow.join(',')}`;
  }
}

export const employeeService = new EmployeeService();

