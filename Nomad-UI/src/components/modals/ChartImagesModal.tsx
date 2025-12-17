'use client';

import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';

interface ChartImagesModalProps {
  projectSlug: string;
  surveyId: string;
  isOpen: boolean;
  onClose: () => void;
}

interface ReportChartImage {
  id?: string;
  Id?: string;
  surveyId?: string;
  SurveyId?: string;
  imageType?: string;
  ImageType?: string;
  clusterName?: string | null;
  ClusterName?: string | null;
  competencyName?: string | null;
  CompetencyName?: string | null;
  imageUrl?: string;
  ImageUrl?: string;
  createdAt?: string;
  CreatedAt?: string;
  updatedAt?: string | null;
  UpdatedAt?: string | null;
}

interface CompetencyChartImage {
  competencyName?: string;
  CompetencyName?: string;
  image?: ReportChartImage | null;
  Image?: ReportChartImage | null;
}

interface ClusterChartImages {
  clusterName?: string;
  ClusterName?: string;
  clusterImage?: ReportChartImage | null;
  ClusterImage?: ReportChartImage | null;
  competencies?: CompetencyChartImage[];
  Competencies?: CompetencyChartImage[];
}

interface ChartImageHierarchy {
  page4Image?: ReportChartImage | null;
  Page4Image?: ReportChartImage | null;
  page6Image?: ReportChartImage | null;
  Page6Image?: ReportChartImage | null;
  clusters?: ClusterChartImages[];
  Clusters?: ClusterChartImages[];
}

// Helper functions to normalize case
const getImageUrl = (img: ReportChartImage | null | undefined): string | undefined =>
  img?.imageUrl || img?.ImageUrl;

const getClusterName = (cluster: ClusterChartImages): string =>
  cluster.clusterName || cluster.ClusterName || '';

const getClusterImage = (cluster: ClusterChartImages): ReportChartImage | null | undefined =>
  cluster.clusterImage || cluster.ClusterImage;

const getCompetencies = (cluster: ClusterChartImages): CompetencyChartImage[] =>
  cluster.competencies || cluster.Competencies || [];

const getCompetencyName = (comp: CompetencyChartImage): string =>
  comp.competencyName || comp.CompetencyName || '';

const getCompetencyImage = (comp: CompetencyChartImage): ReportChartImage | null | undefined =>
  comp.image || comp.Image;

const getClusters = (h: ChartImageHierarchy | null): ClusterChartImages[] =>
  h?.clusters || h?.Clusters || [];

const getPage4Image = (h: ChartImageHierarchy | null): ReportChartImage | null | undefined =>
  h?.page4Image || h?.Page4Image;

const getPage6Image = (h: ChartImageHierarchy | null): ReportChartImage | null | undefined =>
  h?.page6Image || h?.Page6Image;

export default function ChartImagesModal({
  projectSlug,
  surveyId,
  isOpen,
  onClose,
}: ChartImagesModalProps) {
  const { token } = useAuth();
  const [hierarchy, setHierarchy] = useState<ChartImageHierarchy | null>(null);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState<string | null>(null);
  const [expandedClusters, setExpandedClusters] = useState<Set<string>>(new Set());
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [pendingUpload, setPendingUpload] = useState<{
    imageType: string;
    clusterName?: string;
    competencyName?: string;
  } | null>(null);

  useEffect(() => {
    if (isOpen && surveyId) {
      loadHierarchy();
    }
  }, [isOpen, surveyId]);

  const loadHierarchy = async () => {
    if (!token || !surveyId) return;

    try {
      setLoading(true);
      const response = await fetch(
        `/api/${projectSlug}/surveys/${surveyId}/chart-images/hierarchy`,
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );

      if (!response.ok) throw new Error('Failed to load chart images');

      const data = await response.json();
      setHierarchy(data);
    } catch (error) {
      console.error('Error loading chart images:', error);
      toast.error('Failed to load chart images');
    } finally {
      setLoading(false);
    }
  };

  const handleUploadClick = (
    imageType: string,
    clusterName?: string,
    competencyName?: string
  ) => {
    setPendingUpload({ imageType, clusterName, competencyName });
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !pendingUpload || !token) return;

    const uploadKey = `${pendingUpload.imageType}-${pendingUpload.clusterName || ''}-${pendingUpload.competencyName || ''}`;
    setUploading(uploadKey);

    try {
      // First upload to Cloudinary
      const formData = new FormData();
      formData.append('image', file);
      const folder = 'report_templates/chart_images';

      const cloudinaryResponse = await fetch(
        `/api/${projectSlug}/cloudinary/upload?folder=${encodeURIComponent(folder)}`,
        {
          method: 'POST',
          headers: { Authorization: `Bearer ${token}` },
          body: formData,
        }
      );

      if (!cloudinaryResponse.ok) throw new Error('Failed to upload image');

      const cloudinaryData = await cloudinaryResponse.json();
      console.log('Cloudinary response:', cloudinaryData);
      const imageUrl = cloudinaryData.imageUrl || cloudinaryData.SecureUrl || cloudinaryData.secureUrl;

      if (!imageUrl) {
        console.error('No image URL found in cloudinary response:', cloudinaryData);
        throw new Error('Failed to get image URL from cloudinary');
      }

      // Then save to our API
      const payload = {
        imageType: pendingUpload.imageType,
        clusterName: pendingUpload.clusterName || null,
        competencyName: pendingUpload.competencyName || null,
        imageUrl,
      };
      console.log('Saving chart image with payload:', payload);

      const saveResponse = await fetch(
        `/api/${projectSlug}/surveys/${surveyId}/chart-images`,
        {
          method: 'POST',
          headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(payload),
        }
      );

      if (!saveResponse.ok) {
        const errorData = await saveResponse.json().catch(() => ({}));
        console.error('Save chart image failed:', saveResponse.status, errorData);
        throw new Error(`Failed to save chart image: ${saveResponse.status}`);
      }

      toast.success('Chart image uploaded successfully!');
      await loadHierarchy();
    } catch (error) {
      console.error('Error uploading chart image:', error);
      toast.error('Failed to upload chart image');
    } finally {
      setUploading(null);
      setPendingUpload(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleDelete = async (image: ReportChartImage) => {
    if (!token || !confirm('Are you sure you want to delete this image?')) return;

    const imageId = image.id || image.Id;
    if (!imageId) {
      toast.error('Cannot delete: image ID not found');
      return;
    }

    try {
      const response = await fetch(
        `/api/${projectSlug}/surveys/${surveyId}/chart-images/${imageId}`,
        {
          method: 'DELETE',
          headers: { Authorization: `Bearer ${token}` },
        }
      );

      if (!response.ok) throw new Error('Failed to delete image');

      toast.success('Image deleted successfully');
      await loadHierarchy();
    } catch (error) {
      console.error('Error deleting image:', error);
      toast.error('Failed to delete image');
    }
  };

  const toggleCluster = (clusterName: string) => {
    setExpandedClusters((prev) => {
      const next = new Set(prev);
      if (next.has(clusterName)) {
        next.delete(clusterName);
      } else {
        next.add(clusterName);
      }
      return next;
    });
  };

  const renderImageSlot = (
    label: string,
    image: ReportChartImage | null | undefined,
    imageType: string,
    clusterName?: string,
    competencyName?: string
  ) => {
    const uploadKey = `${imageType}-${clusterName || ''}-${competencyName || ''}`;
    const isUploading = uploading === uploadKey;
    const imgUrl = getImageUrl(image);

    return (
      <div className="flex items-center justify-between py-3 px-4 bg-gray-50 rounded-lg">
        <div className="flex items-center gap-3">
          {imgUrl ? (
            <img
              src={imgUrl}
              alt={label}
              className="w-12 h-12 object-cover rounded border border-gray-200"
            />
          ) : (
            <div className="w-12 h-12 bg-gray-200 rounded flex items-center justify-center">
              <svg className="w-6 h-6 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
          )}
          <span className="text-sm font-medium text-gray-700">{label}</span>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => handleUploadClick(imageType, clusterName, competencyName)}
            disabled={isUploading}
            className="px-3 py-1.5 text-xs font-medium text-indigo-600 hover:text-indigo-800 hover:bg-indigo-50 rounded transition-colors disabled:opacity-50"
          >
            {isUploading ? 'Uploading...' : imgUrl ? 'Replace' : 'Upload'}
          </button>
          {image && imgUrl && (
            <button
              onClick={() => handleDelete(image)}
              className="px-3 py-1.5 text-xs font-medium text-red-600 hover:text-red-800 hover:bg-red-50 rounded transition-colors"
            >
              Delete
            </button>
          )}
        </div>
      </div>
    );
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/50 backdrop-blur flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-3xl w-full mx-4 max-h-[85vh] overflow-hidden">
        {/* Header */}
        <div className="p-6 border-b border-gray-200 flex items-center justify-between">
          <div>
            <h2 className="text-xl font-bold text-gray-900">Radial Chart Images</h2>
            <p className="text-sm text-gray-600 mt-1">
              Upload chart images for report pages, clusters, and competencies
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div className="p-6 overflow-y-auto max-h-[65vh]">
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileChange}
            accept="image/*"
            className="hidden"
          />

          {loading ? (
            <div className="flex items-center justify-center py-12">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
            </div>
          ) : !surveyId ? (
            <div className="text-center py-12 text-gray-500">
              Please select a survey first to manage chart images.
            </div>
          ) : (
            <div className="space-y-6">
              {/* Page Images Section */}
              <div>
                <h3 className="text-sm font-semibold text-gray-900 uppercase tracking-wider mb-3">
                  📄 Page Images
                </h3>
                <div className="space-y-2">
                  {renderImageSlot('Page 4 - Radial Chart', getPage4Image(hierarchy), 'Page4')}
                  {renderImageSlot('Page 6 - Radial Chart', getPage6Image(hierarchy), 'Page6')}
                </div>
              </div>

              {/* Clusters Section */}
              <div>
                <h3 className="text-sm font-semibold text-gray-900 uppercase tracking-wider mb-3">
                  📄 Clusters & Competencies
                </h3>
                {getClusters(hierarchy).length > 0 ? (
                  <div className="space-y-2">
                    {getClusters(hierarchy).map((cluster) => {
                      const clusterName = getClusterName(cluster);
                      const clusterImage = getClusterImage(cluster);
                      const competencies = getCompetencies(cluster);

                      return (
                      <div key={clusterName} className="border border-gray-200 rounded-lg overflow-hidden">
                        {/* Cluster Header */}
                        <div
                          className="flex items-center justify-between p-3 bg-gray-100 cursor-pointer hover:bg-gray-150"
                          onClick={() => toggleCluster(clusterName)}
                        >
                          <div className="flex items-center gap-2">
                            <svg
                              className={`w-4 h-4 text-gray-500 transition-transform ${expandedClusters.has(clusterName) ? 'rotate-90' : ''}`}
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                            </svg>
                            <span className="font-medium text-gray-900">{clusterName}</span>
                            <span className="text-xs text-gray-500">
                              ({competencies.length} competencies)
                            </span>
                          </div>
                          {getImageUrl(clusterImage) && (
                            <span className="text-xs text-green-600 font-medium">✓ Has image</span>
                          )}
                        </div>

                        {/* Cluster Content */}
                        {expandedClusters.has(clusterName) && (
                          <div className="p-3 space-y-2">
                            {/* Cluster Image */}
                            {renderImageSlot(
                              `${clusterName} - Cluster Chart`,
                              clusterImage,
                              'Cluster',
                              clusterName
                            )}

                            {/* Competency Images */}
                            {competencies.length > 0 && (
                              <div className="ml-4 mt-3 space-y-2">
                                <p className="text-xs font-medium text-gray-500 uppercase">Competencies</p>
                                {competencies.map((comp) => {
                                  const compName = getCompetencyName(comp);
                                  const compImage = getCompetencyImage(comp);
                                  return (
                                  <div key={compName}>
                                    {renderImageSlot(
                                      compName,
                                      compImage,
                                      'Competency',
                                      clusterName,
                                      compName
                                    )}
                                  </div>
                                  );
                                })}
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                      );
                    })}
                  </div>
                ) : (
                  <div className="text-center py-8 text-gray-500 bg-gray-50 rounded-lg">
                    <p>No clusters found for this survey.</p>
                    <p className="text-xs mt-1">Add questions with clusters to see them here.</p>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-gray-200 bg-gray-50 flex justify-end">
          <button
            onClick={onClose}
            className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-md font-medium transition-colors"
          >
            Done
          </button>
        </div>
      </div>
    </div>
  );
}

