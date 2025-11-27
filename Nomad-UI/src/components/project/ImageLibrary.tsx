/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import toast from 'react-hot-toast';

interface ImageLibraryProps {
  projectSlug: string;
  isOpen: boolean;
  onClose: () => void;
  onSelectImage: (imageUrl: string) => void;
  type: 'logo' | 'cover' | 'general';
}

interface CloudinaryImage {
  publicId: string;
  secureUrl: string;
  fileName: string;
  createdAt: string;
  width?: number;
  height?: number;
  format?: string;
  bytes?: number;
}

export default function ImageLibrary({ projectSlug, isOpen, onClose, onSelectImage, type }: ImageLibraryProps) {
  const { token } = useAuth();
  const [images, setImages] = useState<CloudinaryImage[]>([]);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [selectedImages, setSelectedImages] = useState<string[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const bulkFileInputRef = useRef<HTMLInputElement>(null);

  const folderMap: Record<string, string> = {
    logo: 'report_templates/logos',
    cover: 'report_templates/covers',
    general: 'nomad-surveys'
  };

  const folder = folderMap[type] || 'nomad-surveys';

  useEffect(() => {
    if (isOpen) {
      loadImages();
    }
  }, [isOpen, folder]);

  const loadImages = async () => {
    if (!token) return;

    try {
      setLoading(true);
      const url = `/api/${projectSlug}/cloudinary/images?folder=${encodeURIComponent(folder)}`;
      console.log('Loading images from:', url);
      
      const response = await fetch(url, {
        method: 'GET',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Failed to load images:', response.status, errorText);
        throw new Error('Failed to load images');
      }

      const data = await response.json();
      console.log('Images API response:', data);
      
      // Handle both response structures: { images: [...] } or direct array
      const imagesArray = data.images || data || [];
      console.log('Setting images:', imagesArray.length, 'items');
      
      // Normalize API response: convert PascalCase to camelCase
      const normalizedImages = imagesArray.map((img: any): CloudinaryImage => ({
        publicId: img.PublicId || img.publicId || '',
        secureUrl: img.SecureUrl || img.secureUrl || '',
        fileName: img.FileName || img.fileName || '',
        createdAt: img.CreatedAt || img.createdAt || '',
        width: img.Width || img.width,
        height: img.Height || img.height,
        format: img.Format || img.format,
        bytes: img.Bytes || img.bytes,
      }));
      
      // Filter out any images with invalid URLs
      const validImages = normalizedImages.filter((img: CloudinaryImage) => 
        img && img.secureUrl && img.secureUrl.trim() !== ''
      );
      
      console.log('Valid images after filtering:', validImages.length);
      if (validImages.length !== imagesArray.length) {
        console.warn('Filtered out', imagesArray.length - validImages.length, 'images with invalid URLs');
      }
      
      setImages(validImages);
    } catch (error) {
      console.error('Error loading images:', error);
      toast.error('Failed to load images from library');
      setImages([]);
    } finally {
      setLoading(false);
    }
  };

  const handleUpload = async (files: FileList | null) => {
    if (!files || files.length === 0 || !token) return;

    try {
      setUploading(true);
      const formData = new FormData();
      
      if (files.length === 1) {
        formData.append('image', files[0]);
        const response = await fetch(`/api/${projectSlug}/cloudinary/upload?folder=${encodeURIComponent(folder)}`, {
          method: 'POST',
          headers: {
            Authorization: `Bearer ${token}`,
          },
          body: formData,
        });

        if (!response.ok) {
          const errorData = await response.json();
          throw new Error(errorData.error || 'Failed to upload image');
        }

        toast.success('Image uploaded successfully!');
      } else {
        Array.from(files).forEach(file => {
          formData.append('images', file);
        });
        
        const response = await fetch(`/api/${projectSlug}/cloudinary/upload/bulk?folder=${encodeURIComponent(folder)}`, {
          method: 'POST',
          headers: {
            Authorization: `Bearer ${token}`,
          },
          body: formData,
        });

        if (!response.ok) {
          const errorData = await response.json();
          throw new Error(errorData.error || 'Failed to upload images');
        }

        const data = await response.json();
        toast.success(`Successfully uploaded ${data.summary.successCount} image(s)!`);
      }

      await loadImages();
    } catch (error) {
      console.error('Error uploading images:', error);
      toast.error(error instanceof Error ? error.message : 'Failed to upload images');
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
      if (bulkFileInputRef.current) bulkFileInputRef.current.value = '';
    }
  };

  const handleDelete = async (publicId: string) => {
    if (!token || !confirm('Are you sure you want to delete this image?')) return;

    try {
      const encodedPublicId = encodeURIComponent(publicId);
      const response = await fetch(`/api/${projectSlug}/cloudinary/images/${encodedPublicId}`, {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to delete image');
      }

      toast.success('Image deleted successfully!');
      await loadImages();
    } catch (error) {
      console.error('Error deleting image:', error);
      toast.error('Failed to delete image');
    }
  };

  const handleSelect = (imageUrl: string) => {
    onSelectImage(imageUrl);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-6xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <div>
            <h2 className="text-2xl font-bold text-gray-900">
              {type === 'logo' ? 'Logo Library' : type === 'cover' ? 'Cover Image Library' : 'Image Library'}
            </h2>
            <p className="text-sm text-gray-600 mt-1">Upload and manage your images</p>
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

        {/* Upload Section */}
        <div className="p-6 border-b border-gray-200 bg-gray-50">
          <div className="flex gap-4">
            <div className="flex-1">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Upload Single Image
              </label>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                onChange={(e) => handleUpload(e.target.files)}
                disabled={uploading}
                className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-purple-600 file:text-white hover:file:bg-purple-700 disabled:opacity-50"
              />
            </div>
            <div className="flex-1">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Upload Multiple Images
              </label>
              <input
                ref={bulkFileInputRef}
                type="file"
                accept="image/*"
                multiple
                onChange={(e) => handleUpload(e.target.files)}
                disabled={uploading}
                className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-purple-600 file:text-white hover:file:bg-purple-700 disabled:opacity-50"
              />
            </div>
          </div>
          {uploading && (
            <div className="mt-4 flex items-center gap-2 text-purple-600">
              <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-purple-600"></div>
              <span className="text-sm">Uploading...</span>
            </div>
          )}
        </div>

        {/* Images Grid */}
        <div className="flex-1 overflow-auto p-6">
          {loading ? (
            <div className="flex items-center justify-center h-64">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600"></div>
            </div>
          ) : images.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-64 text-gray-500">
              <svg className="w-16 h-16 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <p className="text-lg font-medium">No images found</p>
              <p className="text-sm">Upload images to get started</p>
              <p className="text-xs text-gray-400 mt-2">Folder: {folder}</p>
            </div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
              {images.map((image) => (
                <div
                  key={image.publicId}
                  className="group relative bg-gray-100 rounded-lg overflow-hidden border-2 border-gray-200 hover:border-purple-500 transition-all cursor-pointer"
                  onClick={() => handleSelect(image.secureUrl)}
                >
                  <img
                    src={image.secureUrl}
                    alt={image.fileName}
                    className="w-full h-48 object-cover"
                    onError={(e) => {
                      console.error('Failed to load image:', image.secureUrl);
                      e.currentTarget.src = 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="200" height="200"%3E%3Crect fill="%23ddd" width="200" height="200"/%3E%3Ctext fill="%23999" font-family="sans-serif" font-size="14" x="50%25" y="50%25" text-anchor="middle" dy=".3em"%3EFailed to load%3C/text%3E%3C/svg%3E';
                    }}
                  />
                  <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-50 transition-all flex items-center justify-center">
                    <div className="opacity-0 group-hover:opacity-100 transition-opacity flex gap-2">
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          handleSelect(image.secureUrl);
                        }}
                        className="px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 text-sm font-medium"
                      >
                        Select
                      </button>
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDelete(image.publicId);
                        }}
                        className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 text-sm font-medium"
                      >
                        Delete
                      </button>
                    </div>
                  </div>
                  <div className="p-2 bg-white">
                    <p className="text-xs text-gray-600 truncate" title={image.fileName}>
                      {image.fileName}
                    </p>
                    {image.width && image.height && (
                      <p className="text-xs text-gray-400">
                        {image.width} × {image.height}
                      </p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

