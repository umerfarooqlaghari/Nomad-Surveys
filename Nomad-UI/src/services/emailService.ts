/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient } from './api';

interface SendPasswordResetOtpRequest {
  email: string;
}

interface VerifyOtpAndResetPasswordRequest {
  email: string;
  otp: string;
  newPassword: string;
}

interface SendFormReminderRequest {
  subjectEvaluatorSurveyId: string;
}

interface EmailResponse {
  message: string;
}

interface BulkEmailRequest {
  subjectEvaluatorSurveyIds: string[];
}

class EmailService {
  /**
   * Send password reset OTP email
   */
  async sendPasswordResetOtp(
    tenantSlug: string,
    email: string
  ): Promise<{ success: boolean; message: string; error?: string }> {
    try {
      const response = await apiClient.post<EmailResponse>(
        `/${tenantSlug}/email/send-password-reset-otp`,
        { email } as SendPasswordResetOtpRequest
      );

      if (response.error) {
        return {
          success: false,
          message: '',
          error: response.error,
        };
      }

      return {
        success: true,
        message: response.data?.message || 'OTP sent successfully',
      };
    } catch (error: any) {
      return {
        success: false,
        message: '',
        error: error.message || 'An error occurred while sending OTP',
      };
    }
  }

  /**
   * Verify OTP and reset password
   */
  async verifyOtpAndResetPassword(
    tenantSlug: string,
    email: string,
    otp: string,
    newPassword: string
  ): Promise<{ success: boolean; message: string; error?: string }> {
    try {
      const response = await apiClient.post<EmailResponse>(
        `/${tenantSlug}/email/verify-otp-and-reset-password`,
        {
          email,
          otp,
          newPassword,
        } as VerifyOtpAndResetPasswordRequest
      );

      if (response.error) {
        return {
          success: false,
          message: '',
          error: response.error,
        };
      }

      return {
        success: true,
        message: response.data?.message || 'Password reset successfully',
      };
    } catch (error: any) {
      return {
        success: false,
        message: '',
        error: error.message || 'An error occurred while resetting password',
      };
    }
  }

  /**
   * Send form reminder email
   */
  async sendFormReminder(
    tenantSlug: string,
    subjectEvaluatorSurveyId: string,
    token: string
  ): Promise<{ success: boolean; message: string; error?: string }> {
    try {
      const response = await apiClient.post<EmailResponse>(
        `/${tenantSlug}/email/send-form-reminder`,
        {
          subjectEvaluatorSurveyId,
        } as SendFormReminderRequest,
        token
      );

      if (response.error) {
        return {
          success: false,
          message: '',
          error: response.error,
        };
      }

      return {
        success: true,
        message: response.data?.message || 'Reminder sent successfully',
      };
    } catch (error: any) {
      return {
        success: false,
        message: '',
        error: error.message || 'An error occurred while sending reminder',
      };
    }
  }

  /**
   * Send bulk form reminder emails
   */
  async sendBulkReminders(
    tenantSlug: string,
    subjectEvaluatorSurveyIds: string[],
    token: string
  ): Promise<{ success: boolean; message: string; error?: string }> {
    try {
      const response = await apiClient.post<EmailResponse>(
        `/${tenantSlug}/email/send-bulk-reminders`,
        { subjectEvaluatorSurveyIds } as BulkEmailRequest,
        token
      );

      if (response.error) {
        return { success: false, message: '', error: response.error };
      }

      return {
        success: true,
        message: response.data?.message || 'Bulk reminders sent successfully',
      };
    } catch (error: any) {
      return {
        success: false,
        message: '',
        error: error.message || 'An error occurred while sending bulk reminders',
      };
    }
  }

  /**
   * Send bulk assignment emails
   */
  async sendBulkAssignments(
    tenantSlug: string,
    subjectEvaluatorSurveyIds: string[],
    token: string
  ): Promise<{ success: boolean; message: string; error?: string }> {
    try {
      const response = await apiClient.post<EmailResponse>(
        `/${tenantSlug}/email/send-bulk-assignments`,
        { subjectEvaluatorSurveyIds } as BulkEmailRequest,
        token
      );

      if (response.error) {
        return { success: false, message: '', error: response.error };
      }

      return {
        success: true,
        message: response.data?.message || 'Bulk assignments sent successfully',
      };
    } catch (error: any) {
      return {
        success: false,
        message: '',
        error: error.message || 'An error occurred while sending bulk assignments',
      };
    }
  }
}

const emailService = new EmailService();
export default emailService;

