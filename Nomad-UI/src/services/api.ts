/* eslint-disable @typescript-eslint/no-explicit-any */
// Base API configuration and utilities - Using Next.js API routes
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || '/api';

export interface ApiResponse<T> {
  data?: T;
  error?: string;
  status: number;
}

export interface ApiError {
  message: string;
  status: number;
  details?: any;
}

class ApiClient {
  public baseURL: string;

  constructor(baseURL: string = API_BASE_URL) {
    this.baseURL = baseURL;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<ApiResponse<T>> {
    const url = `${this.baseURL}${endpoint}`;

    const defaultHeaders: HeadersInit = {
      'Content-Type': 'application/json',
    };

    const config: RequestInit = {
      ...options,
      headers: {
        ...defaultHeaders,
        ...options.headers,
      },
    };

    try {
      const response = await fetch(url, config);

      // Treat 207 Multi-Status as a success case with a response body
      if (response.status === 207) {
        const data = await response.json();
        return {
          data,
          status: response.status,
        };
      }

      if (!response.ok) {
        let errorMessage = `HTTP ${response.status}: ${response.statusText}`;

        try {
          const errorData = await response.json();

          if (errorData.errors && typeof errorData.errors === 'object') {
            // Handle standard ASP.NET validation errors: { "Field": ["Error1", "Error2"] }
            const errorMessages = Object.values(errorData.errors)
              .flat()
              .filter((msg): msg is string => typeof msg === 'string');

            if (errorMessages.length > 0) {
              errorMessage = errorMessages.join(', ');
            } else {
              errorMessage = errorData.message || errorData.title || errorMessage;
            }
          } else {
            errorMessage = errorData.message || errorData.title || errorMessage;
          }
        } catch {
          // If response is not JSON, use the default error message
        }

        return {
          error: errorMessage,
          status: response.status,
        };
      }

      // Handle empty responses (like 204 No Content)
      if (response.status === 204 || response.headers.get('content-length') === '0') {
        return {
          data: undefined as T,
          status: response.status,
        };
      }

      const data = await response.json();
      return {
        data,
        status: response.status,
      };
    } catch (error) {
      return {
        error: error instanceof Error ? error.message : 'Network error occurred',
        status: 0,
      };
    }
  }

  // GET request
  async get<T>(endpoint: string, token?: string): Promise<ApiResponse<T>> {
    const headers: HeadersInit = {};
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    return this.request<T>(endpoint, {
      method: 'GET',
      headers,
    });
  }

  // POST request
  async post<T>(
    endpoint: string,
    data?: any,
    token?: string
  ): Promise<ApiResponse<T>> {
    const headers: HeadersInit = {};
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    return this.request<T>(endpoint, {
      method: 'POST',
      headers,
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  // PUT request
  async put<T>(
    endpoint: string,
    data?: any,
    token?: string
  ): Promise<ApiResponse<T>> {
    const headers: HeadersInit = {};
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    return this.request<T>(endpoint, {
      method: 'PUT',
      headers,
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  // DELETE request
  async delete<T>(endpoint: string, token?: string): Promise<ApiResponse<T>> {
    const headers: HeadersInit = {};
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    return this.request<T>(endpoint, {
      method: 'DELETE',
      headers,
    });
  }

  // PATCH request
  async patch<T>(
    endpoint: string,
    data?: any,
    token?: string
  ): Promise<ApiResponse<T>> {
    const headers: HeadersInit = {};
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    return this.request<T>(endpoint, {
      method: 'PATCH',
      headers,
      body: data ? JSON.stringify(data) : undefined,
    });
  }
}

// Create and export a singleton instance
export const apiClient = new ApiClient();

// Export the class for testing or custom instances
export { ApiClient };

// Utility function to handle API responses with error handling
export function handleApiResponse<T>(
  response: ApiResponse<T>
): { data: T; error: null } | { data: null; error: string } {
  if (response.error) {
    return { data: null, error: response.error };
  }

  // For 204 No Content responses, return success with empty object
  if (response.status === 204 || response.data === undefined) {
    return { data: {} as T, error: null };
  }

  return { data: response.data, error: null };
}

// Utility function to check if an error is a specific HTTP status
export function isApiError(response: ApiResponse<any>, status: number): boolean {
  return response.status === status;
}

// Common HTTP status codes
export const HTTP_STATUS = {
  OK: 200,
  CREATED: 201,
  NO_CONTENT: 204,
  MULTI_STATUS: 207,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  CONFLICT: 409,
  INTERNAL_SERVER_ERROR: 500,
} as const;
