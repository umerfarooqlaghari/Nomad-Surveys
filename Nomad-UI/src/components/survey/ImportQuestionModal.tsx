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
  onImport: (question: Question) => void;
  tenantSlug: string;
  token: string;
}

export default function ImportQuestionModal({
  isOpen,
  onClose,
  onImport,
  tenantSlug,
  token,
}: ImportQuestionModalProps) {
  const [clusters, setClusters] = useState<Cluster[]>([]);
  const [selectedCompetencyId, setSelectedCompetencyId] = useState<string>('');
  const [selectedQuestionId, setSelectedQuestionId] = useState<string>('');
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
    // Find selected question from hierarchical data along with its cluster info
    let selectedQuestion = null;
    let questionClusterId = '';

    for (const cluster of clusters) {
      for (const comp of cluster?.Competencies || []) {
        const question = comp?.Questions?.find((q) => q?.Id === selectedQuestionId);
        if (question) {
          selectedQuestion = question;
          questionClusterId = cluster.Id;
          break;
        }
      }
      if (selectedQuestion) break;
    }

    if (!selectedQuestion) {
      toast.error('Please select a question to import');
      return;
    }

    // Include clusterId in the question object for the import handler
    const questionWithCluster = {
      ...selectedQuestion,
      ClusterId: questionClusterId,
    };

    onImport(questionWithCluster);
    handleClose();
  };

  const handleClusterClick = (clusterId: string) => {
    if (expandedClusterId === clusterId) {
      setExpandedClusterId(null);
    } else {
      setExpandedClusterId(clusterId);
      setSelectedCompetencyId('');
      setSelectedQuestionId('');
    }
  };

  const handleCompetencyClick = (competencyId: string) => {
    if (selectedCompetencyId === competencyId) {
      setSelectedCompetencyId('');
    } else {
      setSelectedCompetencyId(competencyId);
      setSelectedQuestionId('');
    }
  };

  const handleQuestionClick = (questionId: string) => {
    setSelectedQuestionId(questionId);
  };

  const handleClose = () => {
    setSelectedCompetencyId('');
    setSelectedQuestionId('');
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

  // Find selected question from hierarchical data
  const selectedQuestion = filteredClusters
    .flatMap((c) => c?.Competencies || [])
    .flatMap((comp) => comp?.Questions || [])
    .find((q) => q?.Id === selectedQuestionId);

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
                    filteredClusters.map((cluster) => (
                      <div key={cluster.Id} className="border border-gray-300 rounded-lg bg-white">
                        {/* Cluster Item */}
                        <button
                          onClick={() => handleClusterClick(cluster.Id)}
                          className="w-full px-4 py-3 flex items-center justify-between hover:bg-gray-50 transition-colors text-left"
                        >
                          <div className="flex items-center gap-2">
                            <span className="text-lg">
                              {expandedClusterId === cluster.Id ? '' : ''}
                            </span>
                            <span className="font-medium text-black">{cluster.ClusterName}</span>
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
                              (cluster.Competencies || []).map((competency) => (
                                <div key={competency.Id}>
                                  {/* Competency Item */}
                                  <button
                                    onClick={() => handleCompetencyClick(competency.Id)}
                                    className="w-full px-3 py-2 flex items-center justify-between hover:bg-blue-50 rounded transition-colors text-left"
                                  >
                                    <div className="flex items-center gap-2">
                                      <span className="text-base">
                                        {selectedCompetencyId === competency.Id ? '' : ''}
                                      </span>
                                      <span className="text-sm font-medium text-black">
                                        {competency.Name}
                                      </span>
                                    </div>
                                    <span className="text-gray-400 text-sm">
                                      {selectedCompetencyId === competency.Id ? '▼' : '▶'}
                                    </span>
                                  </button>

                                  {/* Questions (shown when competency is selected) */}
                                  {selectedCompetencyId === competency.Id && (
                                    <div className="pl-6 pr-2 pt-1 space-y-1">
                                      {(competency.Questions || []).length === 0 ? (
                                        <div className="text-xs text-gray-500 py-2">
                                          {searchQuery ? 'No matching questions' : 'No questions found'}
                                        </div>
                                      ) : (
                                        (competency.Questions || []).map((question) => (
                                          <button
                                            key={question.Id}
                                            onClick={() => handleQuestionClick(question.Id)}
                                            className={`w-full px-3 py-2 text-left rounded transition-colors ${
                                              selectedQuestionId === question.Id
                                                ? 'bg-blue-100 border-2 border-blue-500'
                                                : 'bg-white border border-gray-200 hover:bg-gray-50'
                                            }`}
                                          >
                                            <div className="flex items-start gap-2">
                                              <span className="text-sm mt-0.5">
                                                {selectedQuestionId === question.Id ? '' : ''}
                                              </span>
                                              <div className="flex-1">
                                                <p className="text-xs font-medium text-black">
                                                  {question.SelfQuestion.substring(0, 100)}
                                                  {question.SelfQuestion.length > 100 ? '...' : ''}
                                                </p>
                                              </div>
                                            </div>
                                          </button>
                                        ))
                                      )}
                                    </div>
                                  )}
                                </div>
                              ))
                            )}
                          </div>
                        )}
                      </div>
                    ))
                  )}
                </div>

                {/* Question Preview */}
                {selectedQuestion && (
                  <div className="mt-4 p-4 bg-blue-50 rounded-lg border border-blue-200">
                    <h4 className="text-sm font-semibold text-black mb-3"> Selected Question:</h4>
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
                disabled={!selectedQuestionId || isLoading}
                className="w-full inline-flex justify-center rounded-lg border border-transparent shadow-sm px-6 py-2.5 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:w-auto sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                 Import Question
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
    </div>
  );
}
