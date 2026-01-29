/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import toast from 'react-hot-toast';

interface Question {
  Id: string;
  CompetencyId: string;
  SelfQuestion: string;
  OthersQuestion: string;
  QuestionType: string;
  ClusterId?: string;
}

interface Competency {
  Id: string;
  Name: string;
  ClusterId: string;
  ClusterName: string;
  IsActive: boolean;
  CreatedAt: string;
  QuestionCount: number;
  Questions?: Question[]; // For hierarchical data
}

interface Cluster {
  Id: string;
  ClusterName: string;
  IsActive: boolean;
  CreatedAt: string;
  CompetencyCount: number;
  Competencies?: Competency[]; // For hierarchical data
}

interface ImportQuestionModalProps {
  isOpen: boolean;
  onClose: () => void;
  onImport: (questions: Question[]) => void; // Changed to bulk
  tenantSlug: string;
  token: string;
  existingQuestionIds?: string[]; // Added to track existing
}

export default function ImportQuestionModal({
  isOpen,
  onClose,
  onImport,
  tenantSlug,
  token,
  existingQuestionIds = [], // Added with default
}: ImportQuestionModalProps) {
  const [clusters, setClusters] = useState<Cluster[]>([]);
  const [selectedCompetencyId, setSelectedCompetencyId] = useState<string>('');
  const [selectedQuestionIds, setSelectedQuestionIds] = useState<Set<string>>(new Set());
  const [expandedClusterId, setExpandedClusterId] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);

  // Load all data on mount
  useEffect(() => {
    if (isOpen) {
      loadClusters();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen]);

  const loadClusters = async () => {
    try {
      setIsLoading(true);

      // Load all clusters, competencies, and questions at once
      const [clustersRes, competenciesRes, questionsRes] = await Promise.all([
        fetch(`/api/${tenantSlug}/clusters`, {
          headers: { Authorization: `Bearer ${token}` },
        }),
        fetch(`/api/${tenantSlug}/competencies`, {
          headers: { Authorization: `Bearer ${token}` },
        }),
        fetch(`/api/${tenantSlug}/questions`, {
          headers: { Authorization: `Bearer ${token}` },
        }),
      ]);

      if (!clustersRes.ok || !competenciesRes.ok || !questionsRes.ok) {
        throw new Error('Failed to load library data');
      }

      const clustersData: Cluster[] = await clustersRes.json();
      const competenciesData: Competency[] = await competenciesRes.json();
      const questionsData: Question[] = await questionsRes.json();

      // Build hierarchical structure
      const clustersWithData = clustersData.map((cluster) => ({
        ...cluster,
        Competencies: competenciesData
          .filter((comp) => comp.ClusterId === cluster.Id)
          .map((comp) => ({
            ...comp,
            Questions: questionsData.filter((q) => q.CompetencyId === comp.Id),
          })),
      }));

      setClusters(clustersWithData);
    } catch (error: any) {
      console.error('Error loading library data:', error);
      toast.error('Failed to load question library');
    } finally {
      setIsLoading(false);
    }
  };



  const handleImport = () => {
    const selectedQuestions: Question[] = [];

    // Find all selected questions from hierarchical data
    for (const cluster of clusters) {
      for (const competency of cluster.Competencies || []) {
        for (const question of competency.Questions || []) {
          if (selectedQuestionIds.has(question.Id)) {
            selectedQuestions.push({
              ...question,
              ClusterId: cluster.Id
            });
          }
        }
      }
    }

    if (selectedQuestions.length === 0) {
      toast.error('Please select questions to import');
      return;
    }

    onImport(selectedQuestions);
    handleClose();
  };

  const handleClusterClick = (clusterId: string) => {
    if (expandedClusterId === clusterId) {
      setExpandedClusterId(null);
    } else {
      setExpandedClusterId(clusterId);
      setSelectedCompetencyId('');
    }
  };

  const handleCompetencyClick = (competencyId: string) => {
    if (selectedCompetencyId === competencyId) {
      setSelectedCompetencyId('');
    } else {
      setSelectedCompetencyId(competencyId);
    }
  };

  const handleQuestionClick = (questionId: string) => {
    if (existingQuestionIds?.includes(questionId)) return;

    setSelectedQuestionIds(prev => {
      const next = new Set(prev);
      if (next.has(questionId)) {
        next.delete(questionId);
      } else {
        next.add(questionId);
      }
      return next;
    });
  };

  const handleClose = () => {
    setSelectedCompetencyId('');
    setSelectedQuestionIds(new Set());
    setExpandedClusterId(null);
    setSearchQuery('');
    onClose();
  };

  // Filter and search through hierarchical data
  const filteredClusters = clusters
    .map((cluster) => {
      const clusterMatches = cluster?.ClusterName?.toLowerCase().includes(searchQuery.toLowerCase()) ?? false;

      const filteredCompetencies = (cluster.Competencies || [])
        .map((competency) => {
          const competencyMatches = competency?.Name?.toLowerCase().includes(searchQuery.toLowerCase()) ?? false;

          const filteredQuestions = (competency.Questions || []).filter((question) =>
            (question?.SelfQuestion?.toLowerCase().includes(searchQuery.toLowerCase()) ?? false) ||
            (question?.OthersQuestion?.toLowerCase().includes(searchQuery.toLowerCase()) ?? false)
          );

          // Include competency if it matches, or if any of its questions match
          if (competencyMatches || filteredQuestions.length > 0 || !searchQuery) {
            return { ...competency, Questions: filteredQuestions };
          }
          return null;
        })
        .filter((comp) => comp !== null) as Competency[];

      // Include cluster if it matches, or if any of its competencies/questions match
      if (clusterMatches || filteredCompetencies.length > 0 || !searchQuery) {
        return { ...cluster, Competencies: filteredCompetencies };
      }
      return null;
    })
    .filter((cluster) => cluster !== null) as Cluster[];

  // Find first selected question for preview
  const firstSelectedId = Array.from(selectedQuestionIds)[0];
  const selectedQuestion = firstSelectedId ? filteredClusters
    .flatMap((c) => c?.Competencies || [])
    .flatMap((comp) => comp?.Questions || [])
    .find((q) => q?.Id === firstSelectedId) : null;

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:block sm:p-0">
        {/* Background overlay with backdrop blur */}
        <div
          className="fixed inset-0 transition-opacity backdrop-blur-sm bg-black/20"
          onClick={handleClose}
        ></div>

        {/* Modal panel */}
        <div className="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-2xl transform transition-all sm:my-8 sm:align-middle sm:max-w-3xl sm:w-full z-50 relative">
          <div className="bg-white px-6 pt-6 pb-4">
            <div className="w-full">
              {/* Header */}
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-xl font-semibold text-black">
                  Import Question from Library
                </h3>
                <button
                  onClick={handleClose}
                  className="text-gray-400 hover:text-gray-600 transition-colors"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              {/* Search Input */}
              <div className="mb-4">
                <div className="relative">
                  <input
                    type="text"
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    placeholder=" Search clusters, competencies, or questions..."
                    className="w-full px-4 py-3 pl-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-400"
                  />
                  <svg
                    className="absolute left-3 top-3.5 w-5 h-5 text-gray-400"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                    />
                  </svg>
                </div>
              </div>

              {/* Hierarchical List */}
              <div className="space-y-2 max-h-[500px] overflow-y-auto border border-gray-200 rounded-lg p-4 bg-gray-50">
                {isLoading ? (
                  <div className="text-center py-8 text-gray-500">Loading...</div>
                ) : filteredClusters.length === 0 ? (
                  <div className="text-center py-8 text-gray-500">
                    {searchQuery ? 'No results found' : 'No clusters found'}
                  </div>
                ) : (
                  filteredClusters.map((cluster) => {
                    const clusterQuestions = (cluster.Competencies || []).flatMap(c => c.Questions || []);
                    const clusterExistingCount = clusterQuestions.filter(q => existingQuestionIds?.includes(q.Id)).length;
                    const isClusterFullyImported = clusterQuestions.length > 0 && clusterExistingCount === clusterQuestions.length;
                    const isClusterPartiallyImported = clusterExistingCount > 0 && clusterExistingCount < clusterQuestions.length;

                    return (
                      <div
                        key={cluster.Id}
                        className={`border rounded-lg transition-colors ${isClusterFullyImported
                            ? 'bg-green-50 border-green-200'
                            : isClusterPartiallyImported
                              ? 'bg-yellow-50 border-yellow-200'
                              : 'bg-white border-gray-300'
                          }`}
                      >
                        {/* Cluster Item */}
                        <button
                          onClick={() => handleClusterClick(cluster.Id)}
                          className="w-full px-4 py-3 flex items-center justify-between hover:bg-black/5 transition-colors text-left"
                        >
                          <div className="flex items-center gap-2">
                            <span className="font-medium text-black">
                              {cluster.ClusterName}
                              {isClusterFullyImported && (
                                <span className="ml-2 text-[10px] font-bold text-green-600 bg-green-100 px-1.5 py-0.5 rounded-full uppercase">
                                  Fully Imported
                                </span>
                              )}
                              {isClusterPartiallyImported && (
                                <span className="ml-2 text-[10px] font-bold text-yellow-600 bg-yellow-100 px-1.5 py-0.5 rounded-full uppercase">
                                  Partially Imported
                                </span>
                              )}
                            </span>
                          </div>
                          <span className="text-gray-400">
                            {expandedClusterId === cluster.Id ? '▼' : '▶'}
                          </span>
                        </button>

                        {/* Competencies (shown when cluster is expanded) */}
                        {expandedClusterId === cluster.Id && (
                          <div className="pl-8 pr-4 pb-2 space-y-1">
                            {(cluster.Competencies || []).length === 0 ? (
                              <div className="text-sm text-gray-500 py-2">
                                {searchQuery ? 'No matching competencies' : 'No competencies found'}
                              </div>
                            ) : (
                              (cluster.Competencies || []).map((competency) => {
                                const compQuestions = competency.Questions || [];
                                const compExistingCount = compQuestions.filter(q => existingQuestionIds?.includes(q.Id)).length;
                                const isCompFullyImported = compQuestions.length > 0 && compExistingCount === compQuestions.length;
                                const isCompPartiallyImported = compExistingCount > 0 && compExistingCount < compQuestions.length;

                                return (
                                  <div key={competency.Id} className="rounded mb-1 overflow-hidden">
                                    {/* Competency Item */}
                                    <button
                                      onClick={() => handleCompetencyClick(competency.Id)}
                                      className={`w-full px-3 py-2 flex items-center justify-between hover:bg-black/5 transition-colors text-left ${isCompFullyImported
                                          ? 'bg-green-100/50'
                                          : isCompPartiallyImported
                                            ? 'bg-yellow-100/50'
                                            : ''
                                        }`}
                                    >
                                      <div className="flex items-center gap-2">
                                        <span className="text-sm font-medium text-black">
                                          {competency.Name}
                                          {isCompFullyImported && (
                                            <span className="ml-2 text-[9px] font-bold text-green-600 bg-green-100 px-1 py-0.5 rounded uppercase">
                                              Full
                                            </span>
                                          )}
                                          {isCompPartiallyImported && (
                                            <span className="ml-2 text-[9px] font-bold text-yellow-600 bg-yellow-100 px-1 py-0.5 rounded uppercase">
                                              Partial
                                            </span>
                                          )}
                                        </span>
                                      </div>
                                      <span className="text-gray-400 text-sm">
                                        {selectedCompetencyId === competency.Id ? '▼' : '▶'}
                                      </span>
                                    </button>

                                    {/* Questions (shown when competency is selected) */}
                                    {selectedCompetencyId === competency.Id && (
                                      <div className="pl-6 pr-2 pt-1 pb-2 space-y-1 bg-black/5 rounded-b">
                                        {(competency.Questions || []).length === 0 ? (
                                          <div className="text-xs text-gray-500 py-2">
                                            {searchQuery ? 'No matching questions' : 'No questions found'}
                                          </div>
                                        ) : (
                                          (competency.Questions || []).map((question) => {
                                            const isSelected = selectedQuestionIds.has(question.Id);
                                            const isAlreadyInSurvey = existingQuestionIds?.includes(question.Id);

                                            return (
                                              <button
                                                key={question.Id}
                                                onClick={() => handleQuestionClick(question.Id)}
                                                disabled={isAlreadyInSurvey}
                                                className={`w-full px-3 py-2 text-left rounded transition-colors relative mt-1 ${isSelected
                                                  ? 'bg-blue-100 border-2 border-blue-500'
                                                  : isAlreadyInSurvey
                                                    ? 'bg-green-50 border border-green-200 opacity-80 cursor-default'
                                                    : 'bg-white border border-gray-200 hover:bg-gray-50'
                                                  }`}
                                              >
                                                <div className="flex items-start gap-3">
                                                  <div className="mt-0.5 flex-shrink-0">
                                                    {isAlreadyInSurvey ? (
                                                      <div className="w-5 h-5 flex items-center justify-center bg-green-500 rounded text-white">
                                                        <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                                                        </svg>
                                                      </div>
                                                    ) : (
                                                      <div className={`w-5 h-5 rounded border ${isSelected ? 'bg-blue-600 border-blue-600' : 'border-gray-300 bg-white'} flex items-center justify-center`}>
                                                        {isSelected && (
                                                          <svg className="w-3.5 h-3.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                                                          </svg>
                                                        )}
                                                      </div>
                                                    )}
                                                  </div>
                                                  <div className="flex-1">
                                                    <p className={`text-xs font-medium ${isAlreadyInSurvey ? 'text-green-800' : 'text-black'}`}>
                                                      {question.SelfQuestion.substring(0, 100)}
                                                      {question.SelfQuestion.length > 100 ? '...' : ''}
                                                      {isAlreadyInSurvey && (
                                                        <span className="ml-2 inline-flex items-center text-[10px] font-bold text-green-600 bg-green-100 px-1.5 py-0.5 rounded-full">
                                                          ALREADY IN SURVEY
                                                        </span>
                                                      )}
                                                    </p>
                                                  </div>
                                                </div>
                                              </button>
                                            );
                                          })
                                        )}
                                      </div>
                                    )}
                                  </div>
                                );
                              })
                            )}
                          </div>
                        )}
                      </div>
                    );
                  })
                )}
              </div>

              {/* Question Preview */}
              {selectedQuestion && (
                <div className="mt-4 p-4 bg-blue-50 rounded-lg border border-blue-200">
                  <h4 className="text-sm font-semibold text-black mb-3">
                    {selectedQuestionIds.size > 1
                      ? `Selected (${selectedQuestionIds.size}) Questions:`
                      : 'Selected Question Preview:'}
                  </h4>
                  <div className="space-y-3">
                    <div>
                      <span className="inline-block px-2 py-1 text-xs font-semibold text-blue-700 bg-blue-100 rounded mb-1">
                        Self
                      </span>
                      <p className="text-sm text-black">{selectedQuestion.SelfQuestion}</p>
                    </div>
                    <div>
                      <span className="inline-block px-2 py-1 text-xs font-semibold text-green-700 bg-green-100 rounded mb-1">
                        Others
                      </span>
                      <p className="text-sm text-black">{selectedQuestion.OthersQuestion}</p>
                    </div>
                    <div className="text-xs text-gray-600">
                      Type: <span className="font-medium">{selectedQuestion.QuestionType}</span>
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="bg-gray-50 px-4 py-3 mt-4 -mx-6 -mb-4 sm:flex sm:flex-row-reverse gap-3">
              <button
                type="button"
                onClick={handleImport}
                disabled={selectedQuestionIds.size === 0 || isLoading}
                className="w-full inline-flex justify-center rounded-lg border border-transparent shadow-sm px-6 py-2.5 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:w-auto sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                Import Selected ({selectedQuestionIds.size})
              </button>
              <button
                type="button"
                onClick={handleClose}
                className="mt-3 w-full inline-flex justify-center rounded-lg border border-gray-300 shadow-sm px-6 py-2.5 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 sm:mt-0 sm:w-auto sm:text-sm transition-colors"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      </div>
    </div >
  );
}
