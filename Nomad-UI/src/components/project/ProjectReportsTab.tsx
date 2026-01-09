'use client';

import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';
import ImageLibrary from './ImageLibrary';
import ChartImagesModal from '../modals/ChartImagesModal';

interface ProjectReportsTabProps {
  projectSlug: string;
}

interface LoadedTemplateSettings {
  companyName: string;
  companyLogoUrl: string | null;
  coverImageUrl: string | null;
  primaryColor: string;
  secondaryColor: string;
  tertiaryColor: string;
}

interface SurveyListItem {
  Id: string;
  Title: string;
  Description?: string;
  IsActive: boolean;
}

interface SubjectFromSurvey {
  SubjectId: string;
  SubjectFullName: string;
  SubjectEmail: string;
  SubjectEmployeeIdString: string;
  SubjectDesignation?: string;
}

export default function ProjectReportsTab({ projectSlug }: ProjectReportsTabProps) {
  const { token } = useAuth();
  const [companyName, setCompanyName] = useState('Alpha Devs');
  const [companyLogo, setCompanyLogo] = useState<File | null>(null);
  const [logoPreview, setLogoPreview] = useState<string>('');
  const [coverImage, setCoverImage] = useState<File | null>(null);
  const [coverImagePreview, setCoverImagePreview] = useState<string>('');
  const [primaryColor, setPrimaryColor] = useState('#0455A4');
  const [secondaryColor, setSecondaryColor] = useState('#1D8F6C');
  const [tertiaryColor, setTertiaryColor] = useState('#6C757D');
  const [previewHtml, setPreviewHtml] = useState<string>('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [isSavingTemplate, setIsSavingTemplate] = useState(false);
  const [isLoadingTemplate, setIsLoadingTemplate] = useState(true);
  const [loadedTemplate, setLoadedTemplate] = useState<LoadedTemplateSettings | null>(null);
  const [showImageLibrary, setShowImageLibrary] = useState(false);
  const [imageLibraryType, setImageLibraryType] = useState<'logo' | 'cover' | 'general'>('general');
  const [showMenu, setShowMenu] = useState(false);
  const [showPageImagesModal, setShowPageImagesModal] = useState(false);
  const [showDownloadMenu, setShowDownloadMenu] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const downloadMenuRef = useRef<HTMLDivElement>(null);
  const previewRef = useRef<HTMLIFrameElement>(null);

  // Survey and Subject selection state
  const [surveys, setSurveys] = useState<SurveyListItem[]>([]);
  const [selectedSurveyId, setSelectedSurveyId] = useState<string>('');
  const [subjectsForSurvey, setSubjectsForSurvey] = useState<SubjectFromSurvey[]>([]);
  const [selectedSubjectId, setSelectedSubjectId] = useState<string>('');
  const [isLoadingSurveys, setIsLoadingSurveys] = useState(false);
  const [isLoadingSubjects, setIsLoadingSubjects] = useState(false);

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setShowMenu(false);
      }
      if (downloadMenuRef.current && !downloadMenuRef.current.contains(event.target as Node)) {
        setShowDownloadMenu(false);
      }
    };

    if (showMenu || showDownloadMenu) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [showMenu, showDownloadMenu]);

  const handleLogoSelect = (imageUrl: string) => {
    console.log('Logo selected:', imageUrl);
    setLogoPreview(imageUrl);
    setCompanyLogo(null); // Clear file reference since we're using URL
    // The useEffect will automatically trigger updatePreview when logoPreview changes
  };

  const handleCoverImageSelect = (imageUrl: string) => {
    console.log('Cover image selected:', imageUrl);
    setCoverImagePreview(imageUrl);
    setCoverImage(null); // Clear file reference since we're using URL
    // The useEffect will automatically trigger updatePreview when coverImagePreview changes
  };

  const openImageLibrary = (type: 'logo' | 'cover' | 'general') => {
    setImageLibraryType(type);
    setShowImageLibrary(true);
    setShowMenu(false);
  };

  const updatePreview = async () => {
    try {
      setIsGenerating(true);

      const logoUrl = logoPreview || '';
      const coverImageUrl = coverImagePreview || '';

      const previewData = {
        companyName,
        companyLogoUrl: logoUrl,
        coverImageUrl: coverImageUrl,
        primaryColor,
        secondaryColor,
        tertiaryColor,
        surveyId: selectedSurveyId || null,
        subjectId: selectedSubjectId || null,
      };


      const response = await fetch(`/api/${projectSlug}/reports/preview`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(previewData),
      });

      if (!response.ok) {
        let errorMessage = `Failed to generate preview: ${response.status}`;
        try {
          const contentType = response.headers.get('content-type');
          if (contentType?.includes('application/json')) {
            const errorJson = await response.json();
            errorMessage = errorJson.error || errorJson.message || errorMessage;
          } else {
            errorMessage = await response.text();
          }
        } catch {
          errorMessage = `HTTP ${response.status}: ${response.statusText}`;
        }
        console.error('Preview API Error:', response.status, errorMessage);
        throw new Error(errorMessage);
      }

      const html = await response.text();
      setPreviewHtml(html);
    } catch (error) {
      console.error('Error updating preview:', error);
      toast.error('Failed to update preview. Check console for details.');
    } finally {
      setIsGenerating(false);
    }
  };

  // Load saved template settings on mount
  const loadTemplateSettings = async () => {
    if (!token) {
      setIsLoadingTemplate(false);
      return;
    }

    try {
      setIsLoadingTemplate(true);
      const response = await fetch(`/api/${projectSlug}/report-template-settings/default`, {
        method: 'GET',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const data = await response.json();
        if (data) {
          // API returns PascalCase, map to our interface
          const loaded: LoadedTemplateSettings = {
            companyName: data.CompanyName || data.companyName || 'Alpha Devs',
            companyLogoUrl: data.CompanyLogoUrl || data.companyLogoUrl || null,
            coverImageUrl: data.CoverImageUrl || data.coverImageUrl || null,
            primaryColor: data.PrimaryColor || data.primaryColor || '#0455A4',
            secondaryColor: data.SecondaryColor || data.secondaryColor || '#1D8F6C',
            tertiaryColor: data.TertiaryColor || data.tertiaryColor || '#6C757D',
          };

          console.log('Raw API response:', data);
          console.log('Mapped template settings:', loaded);

          setLoadedTemplate(loaded);

          // Set current values from loaded template
          setCompanyName(loaded.companyName);
          setPrimaryColor(loaded.primaryColor);
          setSecondaryColor(loaded.secondaryColor);
          setTertiaryColor(loaded.tertiaryColor);

          // Load images from Cloudinary URLs
          if (loaded.companyLogoUrl) {
            setLogoPreview(loaded.companyLogoUrl);
          } else {
            setLogoPreview('');
          }
          if (loaded.coverImageUrl) {
            setCoverImagePreview(loaded.coverImageUrl);
          } else {
            setCoverImagePreview('');
          }

          console.log('Template settings loaded and state updated:', {
            companyName: loaded.companyName,
            primaryColor: loaded.primaryColor,
            secondaryColor: loaded.secondaryColor,
            tertiaryColor: loaded.tertiaryColor,
            hasLogo: !!loaded.companyLogoUrl,
            hasCoverImage: !!loaded.coverImageUrl,
          });

          // Explicitly trigger preview update after a short delay to ensure state is set
          setTimeout(() => {
            updatePreview();
          }, 100);
        }
      } else if (response.status === 404) {
        // No template saved yet, use defaults
        console.log('No saved template found, using defaults');
      } else {
        console.error('Failed to load template settings:', response.status);
      }
    } catch (error) {
      console.error('Error loading template settings:', error);
    } finally {
      setIsLoadingTemplate(false);
    }
  };

  // Check if there are any changes from the loaded template
  const hasChanges = (): boolean => {
    if (isLoadingTemplate) {
      return false; // Don't show changes while loading
    }

    if (!loadedTemplate) {
      // If no template was loaded, check against defaults
      const hasDefaultChanges =
        companyName !== 'Alpha Devs' ||
        primaryColor !== '#0455A4' ||
        secondaryColor !== '#1D8F6C' ||
        tertiaryColor !== '#6C757D' ||
        logoPreview !== '' ||
        coverImagePreview !== '' ||
        companyLogo !== null ||
        coverImage !== null;
      return hasDefaultChanges;
    }

    // Compare current values with loaded template
    const nameChanged = companyName !== (loadedTemplate.companyName || 'Alpha Devs');
    const primaryColorChanged = primaryColor !== (loadedTemplate.primaryColor || '#0455A4');
    const secondaryColorChanged = secondaryColor !== (loadedTemplate.secondaryColor || '#1D8F6C');
    const tertiaryColorChanged = tertiaryColor !== (loadedTemplate.tertiaryColor || '#6C757D');

    // Check if logo changed
    // - New file uploaded
    // - Preview URL changed (and it's not the same as loaded URL)
    // - Logo was removed (had URL before, now empty)
    const logoChanged = Boolean(
      companyLogo !== null || // New file uploaded
      (logoPreview !== '' && logoPreview !== (loadedTemplate.companyLogoUrl || '')) || // URL changed
      (loadedTemplate.companyLogoUrl && logoPreview === '') // Logo removed
    );

    // Check if cover image changed
    const coverImageChanged = Boolean(
      coverImage !== null || // New file uploaded
      (coverImagePreview !== '' && coverImagePreview !== (loadedTemplate.coverImageUrl || '')) || // URL changed
      (loadedTemplate.coverImageUrl && coverImagePreview === '') // Cover image removed
    );

    return Boolean(
      nameChanged || primaryColorChanged || secondaryColorChanged ||
      tertiaryColorChanged || logoChanged || coverImageChanged
    );
  };

  useEffect(() => {
    loadTemplateSettings();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [projectSlug, token]);

  useEffect(() => {
    if (!isLoadingTemplate) {
      updatePreview();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [companyName, logoPreview, coverImagePreview, primaryColor, secondaryColor, tertiaryColor, isLoadingTemplate, selectedSurveyId, selectedSubjectId]);

  // Fetch surveys on mount
  useEffect(() => {
    const fetchSurveys = async () => {
      if (!token) return;

      setIsLoadingSurveys(true);
      try {
        const response = await fetch(`/api/${projectSlug}/surveys`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (response.ok) {
          const data = await response.json();
          setSurveys(data.filter((s: SurveyListItem) => s.IsActive));
        } else {
          console.error('Failed to fetch surveys');
        }
      } catch (error) {
        console.error('Error fetching surveys:', error);
      } finally {
        setIsLoadingSurveys(false);
      }
    };

    fetchSurveys();
  }, [projectSlug, token]);

  // Fetch subjects when survey changes
  useEffect(() => {
    const fetchSubjectsForSurvey = async () => {
      if (!token || !selectedSurveyId) {
        setSubjectsForSurvey([]);
        setSelectedSubjectId('');
        return;
      }

      setIsLoadingSubjects(true);
      try {
        const response = await fetch(`/api/${projectSlug}/surveys/${selectedSurveyId}/assigned-relationships`, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (response.ok) {
          const data = await response.json();
          // Extract unique subjects from the assigned relationships
          const uniqueSubjects = new Map<string, SubjectFromSurvey>();
          data.forEach((rel: { SubjectId: string; SubjectFullName: string; SubjectEmail: string; SubjectEmployeeIdString: string; SubjectDesignation?: string }) => {
            if (!uniqueSubjects.has(rel.SubjectId)) {
              uniqueSubjects.set(rel.SubjectId, {
                SubjectId: rel.SubjectId,
                SubjectFullName: rel.SubjectFullName,
                SubjectEmail: rel.SubjectEmail,
                SubjectEmployeeIdString: rel.SubjectEmployeeIdString,
                SubjectDesignation: rel.SubjectDesignation,
              });
            }
          });
          setSubjectsForSurvey(Array.from(uniqueSubjects.values()));
        } else {
          console.error('Failed to fetch subjects for survey');
          setSubjectsForSurvey([]);
        }
      } catch (error) {
        console.error('Error fetching subjects for survey:', error);
        setSubjectsForSurvey([]);
      } finally {
        setIsLoadingSubjects(false);
      }
    };

    fetchSubjectsForSurvey();
  }, [projectSlug, token, selectedSurveyId]);

  const handleSaveTemplate = async () => {
    if (!token) {
      toast.error('Authentication token not found. Please log in again.');
      return;
    }

    try {
      setIsSavingTemplate(true);

      // Create FormData for multipart/form-data request
      const formData = new FormData();
      formData.append('name', 'Report Template');
      formData.append('description', 'Saved report template with custom branding');
      formData.append('companyName', companyName);
      formData.append('primaryColor', primaryColor);
      formData.append('secondaryColor', secondaryColor);
      formData.append('tertiaryColor', tertiaryColor);
      formData.append('isDefault', 'true');

      // Add image URLs (images should be uploaded to library first)
      if (logoPreview) {
        formData.append('companyLogoUrl', logoPreview);
      }

      if (coverImagePreview) {
        formData.append('coverImageUrl', coverImagePreview);
      }

      const response = await fetch(`/api/${projectSlug}/report-template-settings/save`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          // Don't set Content-Type - browser will set it with boundary for multipart/form-data
        },
        body: formData,
      });

      if (!response.ok) {
        let errorMessage = 'Failed to save template';
        try {
          const errorData = await response.json();
          errorMessage = errorData.error || errorData.message || errorMessage;
        } catch {
          errorMessage = `HTTP ${response.status}: ${response.statusText}`;
        }
        throw new Error(errorMessage);
      }

      const result = await response.json();

      // Handle response that may contain Template object or be the template directly
      const template = result.Template || result;
      const uploadErrors = result.UploadErrors || [];
      const message = result.Message;

      // Show appropriate toast messages
      if (uploadErrors.length > 0) {
        // Show error for each failed upload
        uploadErrors.forEach((error: string) => {
          toast.error(error, { duration: 5000 });
        });
        if (message) {
          toast(message, { icon: '⚠️', duration: 4000 });
        }
      } else {
        toast.success('Template saved successfully!');
      }

      console.log('Template saved response:', result);
      console.log('Image URLs in response:', {
        companyLogoUrl: template.CompanyLogoUrl || template.companyLogoUrl,
        coverImageUrl: template.CoverImageUrl || template.coverImageUrl,
      });

      // API returns PascalCase, handle both cases
      const savedCompanyName = template.CompanyName || template.companyName;
      const savedPrimaryColor = template.PrimaryColor || template.primaryColor;
      const savedSecondaryColor = template.SecondaryColor || template.secondaryColor;
      const savedTertiaryColor = template.TertiaryColor || template.tertiaryColor;
      const savedCompanyLogoUrl = template.CompanyLogoUrl || template.companyLogoUrl;
      const savedCoverImageUrl = template.CoverImageUrl || template.coverImageUrl;

      // Update all state values from the API response to ensure UI matches saved data
      if (savedCompanyName) {
        setCompanyName(savedCompanyName);
      }
      if (savedPrimaryColor) {
        setPrimaryColor(savedPrimaryColor);
      }
      if (savedSecondaryColor) {
        setSecondaryColor(savedSecondaryColor);
      }
      if (savedTertiaryColor) {
        setTertiaryColor(savedTertiaryColor);
      }

      // Update image previews from Cloudinary URLs
      if (savedCompanyLogoUrl) {
        setLogoPreview(savedCompanyLogoUrl);
      } else {
        // If logo was removed, clear preview
        setLogoPreview('');
      }

      if (savedCoverImageUrl) {
        setCoverImagePreview(savedCoverImageUrl);
      } else {
        // If cover image was removed, clear preview
        setCoverImagePreview('');
      }

      // Update loaded template to reflect saved state (for change detection)
      const updatedLoaded: LoadedTemplateSettings = {
        companyName: savedCompanyName || companyName,
        companyLogoUrl: savedCompanyLogoUrl || null,
        coverImageUrl: savedCoverImageUrl || null,
        primaryColor: savedPrimaryColor || primaryColor,
        secondaryColor: savedSecondaryColor || secondaryColor,
        tertiaryColor: savedTertiaryColor || tertiaryColor,
      };
      setLoadedTemplate(updatedLoaded);

      // Show success message for successful uploads
      if (uploadErrors.length === 0) {
        if (savedCompanyLogoUrl || savedCoverImageUrl) {
          const uploadedImages = [];
          if (savedCompanyLogoUrl) uploadedImages.push('logo');
          if (savedCoverImageUrl) uploadedImages.push('cover image');
          toast.success(`Successfully uploaded ${uploadedImages.join(' and ')} to Cloudinary!`, { duration: 3000 });
        }
      }

      // Clear file references since they're now saved
      setCompanyLogo(null);
      setCoverImage(null);

      // Reload template settings to ensure everything is in sync
      // This ensures we have the latest data from the database
      await loadTemplateSettings();
    } catch (error) {
      console.error('Error saving template:', error);
      toast.error(error instanceof Error ? error.message : 'Failed to save template');
    } finally {
      setIsSavingTemplate(false);
    }
  };

  return (
    <>
      <div className="flex h-[calc(100vh-200px)] gap-4">
        {/* Preview Panel - Left Side */}
        <div className="flex-1 flex flex-col bg-gray-50">
          <div className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
            <div>
              <h2 className="text-xl font-bold text-gray-900">Report Preview</h2>
              <p className="text-sm text-gray-600 mt-1">Live preview of your report design</p>
            </div>
          </div>

          <div className="flex-1 overflow-auto p-6 flex justify-center bg-gray-100">
            {previewHtml ? (
              <iframe
                ref={previewRef}
                srcDoc={previewHtml}
                className="w-full max-w-[210mm] bg-white shadow-lg border border-gray-300"
                style={{ height: '297mm', minHeight: '100%' }}
                title="Report Preview"
              />
            ) : (
              <div className="flex items-center justify-center h-full">
                <div className="text-center">
                  <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
                  <p className="text-gray-600">Loading preview...</p>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Settings Panel - Right Sidebar */}
        <div className="w-80 bg-white border-l border-gray-200 flex flex-col overflow-y-auto">
          <div className="p-6 border-b border-gray-200">
            <h2 className="text-xl font-bold text-gray-900 mb-1">Report Settings</h2>
            <p className="text-sm text-gray-600">Customize your report design</p>
          </div>

          <div className="flex-1 p-6 space-y-6">
            {/* Save Template Button - Prominently placed at the top */}
            <div className="pb-4 border-b border-gray-200">
              <button
                onClick={handleSaveTemplate}
                disabled={isSavingTemplate || isLoadingTemplate || !hasChanges()}
                className="w-full bg-purple-600 hover:bg-purple-700 disabled:bg-gray-400 disabled:cursor-not-allowed text-white px-4 py-3 rounded-md font-semibold transition-colors flex items-center justify-center gap-2 shadow-lg hover:shadow-xl"
              >
                {isSavingTemplate ? (
                  <>
                    <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white"></div>
                    <span>Saving Template...</span>
                  </>
                ) : isLoadingTemplate ? (
                  <>
                    <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white"></div>
                    <span>Loading Template...</span>
                  </>
                ) : !hasChanges() ? (
                  <>
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                    <span>No Changes to Save</span>
                  </>
                ) : (
                  <>
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7H5a2 2 0 00-2 2v9a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-3m-1 4l-3-3m0 0l-3 3m3-3v12" />
                    </svg>
                    <span>Save Template</span>
                  </>
                )}
              </button>
              <p className="mt-2 text-xs text-gray-500 text-center">
                {isLoadingTemplate
                  ? 'Loading saved template...'
                  : hasChanges()
                    ? 'You have unsaved changes'
                    : 'All changes saved'}
              </p>
            </div>



            {/* Download Dropdown */}
            <div className="relative pt-4 border-t border-gray-200">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Download Report Data
              </label>
              <div className="relative">
                <button
                  onClick={() => setShowDownloadMenu(!showDownloadMenu)}
                  className="w-full bg-gray-100 hover:bg-gray-200 text-gray-900 border border-gray-300 px-3 py-2 rounded-md font-medium transition-colors flex items-center justify-between"
                >
                  <div className="flex items-center gap-2">
                    <svg className="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                    </svg>
                    <span>Download Options</span>
                  </div>
                  <svg className={`w-5 h-5 text-gray-500 transition-transform ${showDownloadMenu ? 'transform rotate-180' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                  </svg>
                </button>

                {showDownloadMenu && (
                  <div ref={downloadMenuRef} className="absolute z-20 mt-1 w-full bg-white rounded-md shadow-lg border border-gray-200 overflow-hidden">
                    <div className="py-1">
                      <button
                        onClick={async () => {
                          if (!selectedSubjectId) {
                            toast.error('Please select a subject first');
                            return;
                          }

                          try {
                            // Close menu immediately
                            setShowDownloadMenu(false);

                            const toastId = toast.loading('Generating PDF, It may take upto a 2 minutes to generate one');

                            const reportData = {
                              subjectId: selectedSubjectId,
                              surveyId: selectedSurveyId || null,
                              companyName,
                              companyLogoUrl: logoPreview || null,
                              coverImageUrl: coverImagePreview || null,
                              primaryColor,
                              secondaryColor,
                              tertiaryColor,
                            };

                            const response = await fetch(`/api/${projectSlug}/reports/generate/pdf`, {
                              method: 'POST',
                              headers: {
                                'Content-Type': 'application/json',
                                Authorization: `Bearer ${token}`,
                              },
                              body: JSON.stringify(reportData),
                            });

                            if (!response.ok) {
                              throw new Error('Failed to generate PDF');
                            }

                            // Create a blob from the response and trigger download
                            const blob = await response.blob();
                            const url = window.URL.createObjectURL(blob);
                            const a = document.createElement('a');
                            a.href = url;
                            // Try to get filename from Content-Disposition header
                            const contentDisposition = response.headers.get('Content-Disposition');
                            let fileName = `report-${selectedSubjectId}.pdf`;
                            if (contentDisposition) {
                              const match = contentDisposition.match(/filename="?([^"]+)"?/);
                              if (match && match[1]) {
                                fileName = match[1];
                              }
                            }

                            a.download = fileName;
                            document.body.appendChild(a);
                            a.click();
                            window.URL.revokeObjectURL(url);
                            document.body.removeChild(a);

                            toast.success('PDF Downloaded successfully', { id: toastId });
                          } catch (error) {
                            console.error('Error downloading PDF:', error);
                            toast.error('Failed to download PDF');
                          }
                        }}
                        className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center gap-2"
                      >
                        <svg className="w-4 h-4 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                        </svg>
                        Download PDF
                      </button>
                      <button
                        onClick={async () => {
                          if (!selectedSurveyId) {
                            toast.error('Please select a survey first');
                            return;
                          }

                          try {
                            setShowDownloadMenu(false);
                            const toastId = toast.loading('Generating Ratee Average Excel Report...');

                            const response = await fetch(`/api/${projectSlug}/reports/generate/excel/ratee-average`, {
                              method: 'POST',
                              headers: {
                                'Content-Type': 'application/json',
                                Authorization: `Bearer ${token}`,
                              },
                              body: JSON.stringify({
                                surveyId: selectedSurveyId,
                              }),
                            });

                            if (!response.ok) {
                              throw new Error('Failed to generate Excel report');
                            }

                            const blob = await response.blob();
                            const url = window.URL.createObjectURL(blob);
                            const a = document.createElement('a');
                            a.href = url;

                            // Get filename from header or use default
                            const contentDisposition = response.headers.get('Content-Disposition');
                            let fileName = 'Ratee Average Report.xlsx';
                            if (contentDisposition) {
                              const match = contentDisposition.match(/filename="?([^"]+)"?/);
                              if (match && match[1]) fileName = match[1];
                            }

                            a.download = fileName;
                            document.body.appendChild(a);
                            a.click();
                            window.URL.revokeObjectURL(url);
                            document.body.removeChild(a);

                            toast.success('Excel Report Downloaded successfully', { id: toastId });
                          } catch (error) {
                            console.error('Error downloading Excel report:', error);
                            toast.error('Failed to download Excel report');
                          }
                        }}
                        className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center gap-2"
                      >
                        <svg className="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                        </svg>
                        Ratee Average Report
                      </button>
                      <button
                        onClick={() => {
                          console.log('Download Excel 2');
                          setShowDownloadMenu(false);
                          toast.success('Downloading Analytics Sheet 2...');
                        }}
                        className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center gap-2"
                      >
                        <svg className="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                        </svg>
                        Analytics Sheet 2
                      </button>
                      <button
                        onClick={() => {
                          console.log('Download Excel 3');
                          setShowDownloadMenu(false);
                          toast.success('Downloading Analytics Sheet 3...');
                        }}
                        className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center gap-2"
                      >
                        <svg className="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                        </svg>
                        Analytics Sheet 3
                      </button>
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Survey Selection Dropdown */}
            <div className="pt-4 border-t border-gray-200">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Select Survey
              </label>
              <select
                value={selectedSurveyId}
                onChange={(e) => {
                  setSelectedSurveyId(e.target.value);
                  setSelectedSubjectId(''); // Reset subject when survey changes
                }}
                disabled={isLoadingSurveys}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm text-black bg-white"
              >
                <option value="">
                  {isLoadingSurveys ? 'Loading surveys...' : '-- Select a Survey --'}
                </option>
                {surveys.map((survey) => (
                  <option key={survey.Id} value={survey.Id}>
                    {survey.Title}
                  </option>
                ))}
              </select>
            </div>

            {/* Subject Selection Dropdown */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Select Subject
              </label>
              <select
                value={selectedSubjectId}
                onChange={(e) => setSelectedSubjectId(e.target.value)}
                disabled={!selectedSurveyId || isLoadingSubjects}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm text-black bg-white disabled:bg-gray-100 disabled:cursor-not-allowed"
              >
                <option value="">
                  {!selectedSurveyId
                    ? '-- Select a Survey first --'
                    : isLoadingSubjects
                      ? 'Loading subjects...'
                      : subjectsForSurvey.length === 0
                        ? '-- No subjects assigned --'
                        : '-- Select a Subject --'}
                </option>
                {subjectsForSurvey.map((subject) => (
                  <option key={subject.SubjectId} value={subject.SubjectId}>
                    {subject.SubjectFullName} ({subject.SubjectEmployeeIdString})
                  </option>
                ))}
              </select>
            </div>

            {/* Company Name */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Company Name
              </label>
              <input
                type="text"
                value={companyName}
                onChange={(e) => setCompanyName(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-black"
                placeholder="Enter company name"
              />
            </div>

            {/* Logo Selection */}
            <div className="relative">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Company Logo (Header)
              </label>
              <div className="flex gap-2 mb-2">
                <button
                  onClick={() => openImageLibrary('logo')}
                  className="flex-1 px-3 py-2 bg-purple-600 hover:bg-purple-700 text-white rounded-md text-sm font-medium transition-colors flex items-center justify-center gap-2"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                  Choose from Library
                </button>
                <button
                  onClick={() => setShowMenu(!showMenu)}
                  className="px-3 py-2 bg-gray-200 hover:bg-gray-300 text-gray-700 rounded-md text-sm font-medium transition-colors"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                  </svg>
                </button>
              </div>

              {/* Hamburger Menu */}
              {showMenu && (
                <div ref={menuRef} className="absolute z-50 mt-2 w-56 bg-white rounded-md shadow-lg border border-gray-200">
                  <div className="py-1">
                    <button
                      onClick={() => {
                        openImageLibrary('logo');
                      }}
                      className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center gap-2"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                      Logo Library
                    </button>
                    <button
                      onClick={() => {
                        openImageLibrary('cover');
                      }}
                      className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center gap-2"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                      Cover Image Library
                    </button>
                    <button
                      onClick={() => {
                        openImageLibrary('general');
                      }}
                      className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center gap-2"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                      General Image Library
                    </button>
                    <div className="border-t border-gray-200 my-1"></div>
                    <button
                      onClick={() => {
                        const url = prompt('Enter image URL:');
                        if (url) {
                          handleLogoSelect(url);
                        }
                        setShowMenu(false);
                      }}
                      className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center gap-2"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
                      </svg>
                      Enter URL Manually
                    </button>
                  </div>
                </div>
              )}

              <input
                type="text"
                value={logoPreview}
                onChange={(e) => {
                  setLogoPreview(e.target.value);
                  updatePreview();
                }}
                placeholder="Or enter image URL"
                className="w-full mt-2 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm text-black"
              />
              {logoPreview && (
                <div className="mt-3">
                  <img
                    src={logoPreview}
                    alt="Logo preview"
                    className="max-w-32 max-h-32 object-contain border border-gray-300 rounded p-2"
                    onError={(e) => {
                      e.currentTarget.src = '';
                      toast.error('Failed to load image from URL');
                    }}
                  />
                </div>
              )}
            </div>

            {/* Cover Image Selection */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Cover Image (Page 1)
              </label>
              <div className="flex gap-2 mb-2">
                <button
                  onClick={() => openImageLibrary('cover')}
                  className="flex-1 px-3 py-2 bg-purple-600 hover:bg-purple-700 text-white rounded-md text-sm font-medium transition-colors flex items-center justify-center gap-2"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                  Choose from Library
                </button>
                <button
                  onClick={() => {
                    const url = prompt('Enter image URL:');
                    if (url) {
                      handleCoverImageSelect(url);
                    }
                  }}
                  className="px-3 py-2 bg-gray-200 hover:bg-gray-300 text-gray-700 rounded-md text-sm font-medium transition-colors"
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
                  </svg>
                </button>
              </div>
              <input
                type="text"
                value={coverImagePreview}
                onChange={(e) => {
                  setCoverImagePreview(e.target.value);
                  updatePreview();
                }}
                placeholder="Or enter image URL"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm text-black"
              />
              {coverImagePreview && (
                <div className="mt-3">
                  <img
                    src={coverImagePreview}
                    alt="Cover image preview"
                    className="max-w-full h-48 object-cover border border-gray-300 rounded p-2"
                    onError={(e) => {
                      e.currentTarget.src = '';
                      toast.error('Failed to load image from URL');
                    }}
                  />
                </div>
              )}
            </div>

            {/* Chart Images Button */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Radial Chart Images
              </label>
              <button
                onClick={() => setShowPageImagesModal(true)}
                disabled={!selectedSurveyId}
                className="w-full px-3 py-3 bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400 disabled:cursor-not-allowed text-white rounded-md text-sm font-medium transition-colors flex items-center justify-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                Manage Chart Images
              </button>
              <p className="mt-1 text-xs text-gray-500">
                {selectedSurveyId
                  ? 'Upload chart images for pages, clusters, and competencies'
                  : 'Select a survey first to manage chart images'}
              </p>
            </div>

            {/* Primary Color */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Primary Color
              </label>
              <div className="flex gap-2">
                <input
                  type="color"
                  value={primaryColor}
                  onChange={(e) => setPrimaryColor(e.target.value)}
                  className="h-10 w-20 border border-gray-300 rounded cursor-pointer"
                />
                <input
                  type="text"
                  value={primaryColor}
                  onChange={(e) => setPrimaryColor(e.target.value)}
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm text-black"
                />
              </div>
            </div>

            {/* Secondary Color */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Secondary Color
              </label>
              <div className="flex gap-2">
                <input
                  type="color"
                  value={secondaryColor}
                  onChange={(e) => setSecondaryColor(e.target.value)}
                  className="h-10 w-20 border border-gray-300 rounded cursor-pointer"
                />
                <input
                  type="text"
                  value={secondaryColor}
                  onChange={(e) => setSecondaryColor(e.target.value)}
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm text-black"
                />
              </div>
            </div>

            {/* Tertiary Color */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Tertiary Color
              </label>
              <div className="flex gap-2">
                <input
                  type="color"
                  value={tertiaryColor}
                  onChange={(e) => setTertiaryColor(e.target.value)}
                  className="h-10 w-20 border border-gray-300 rounded cursor-pointer"
                />
                <input
                  type="text"
                  value={tertiaryColor}
                  onChange={(e) => setTertiaryColor(e.target.value)}
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm text-black"
                />
              </div>
            </div>

          </div>
        </div>
      </div>

      {/* Image Library Modal */}
      <ImageLibrary
        projectSlug={projectSlug}
        isOpen={showImageLibrary}
        onClose={() => setShowImageLibrary(false)}
        onSelectImage={imageLibraryType === 'logo' ? handleLogoSelect : handleCoverImageSelect}
        type={imageLibraryType}
      />

      {/* Chart Images Modal */}
      <ChartImagesModal
        projectSlug={projectSlug}
        surveyId={selectedSurveyId}
        isOpen={showPageImagesModal}
        onClose={() => setShowPageImagesModal(false)}
      />
    </>
  );
}
