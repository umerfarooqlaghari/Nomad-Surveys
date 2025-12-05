/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { questionService } from '@/services/questionService';
import toast, { Toaster } from 'react-hot-toast';
import {
  ClusterListResponse,
  CompetencyListResponse,
  QuestionListResponse,
  CreateClusterRequest,
  UpdateClusterRequest,
  CreateCompetencyRequest,
  UpdateCompetencyRequest,
  CreateQuestionRequest,
  UpdateQuestionRequest,
} from '@/types/questions';
import styles from './ProjectQuestionsTab.module.css';

interface ProjectQuestionsTabProps {
  projectSlug: string;
}

interface ExpandedState {
  clusters: Set<string>;
  competencies: Set<string>;
}

export default function ProjectQuestionsTab({ projectSlug }: ProjectQuestionsTabProps) {
  const { token } = useAuth();
  const [clusters, setClusters] = useState<ClusterListResponse[]>([]);
  const [competencies, setCompetencies] = useState<CompetencyListResponse[]>([]);
  const [questions, setQuestions] = useState<QuestionListResponse[]>([]);
  const [loading, setLoading] = useState(true);

  // Editing states
  const [editingClusterId, setEditingClusterId] = useState<string | null>(null);
  const [editingCompetencyId, setEditingCompetencyId] = useState<string | null>(null);
  const [editingQuestionId, setEditingQuestionId] = useState<string | null>(null);
  const [isAddingCluster, setIsAddingCluster] = useState(false);
  const [addingCompetencyToCluster, setAddingCompetencyToCluster] = useState<string | null>(null);
  const [addingQuestionToCompetency, setAddingQuestionToCompetency] = useState<string | null>(null);

  // Expanded state
  const [expanded, setExpanded] = useState<ExpandedState>({
    clusters: new Set(),
    competencies: new Set(),
  });

  // Form data
  const [clusterForm, setClusterForm] = useState({ ClusterName: '', Description: '' });
  const [competencyForm, setCompetencyForm] = useState({ Name: '', Description: '', ClusterId: '' });
  const [questionForm, setQuestionForm] = useState({ SelfQuestion: '', OthersQuestion: '', CompetencyId: '' });

  const tenantSlug = projectSlug;

  useEffect(() => {
    if (token) {
      loadAllData();
    }
  }, [token, projectSlug]);

  const loadAllData = async () => {
    if (!token) return;

    setLoading(true);

    try {
      const [clustersRes, competenciesRes, questionsRes] = await Promise.all([
        questionService.getClusters(tenantSlug, token),
        questionService.getCompetencies(tenantSlug, token),
        questionService.getQuestions(tenantSlug, token),
      ]);

      if (clustersRes.error) {
        toast.error(`Failed to load clusters: ${clustersRes.error}`);
      } else {
        setClusters(clustersRes.data || []);
      }

      if (competenciesRes.error) {
        toast.error(`Failed to load competencies: ${competenciesRes.error}`);
      } else {
        setCompetencies(competenciesRes.data || []);
      }

      if (questionsRes.error) {
        toast.error(`Failed to load questions: ${questionsRes.error}`);
      } else {
        setQuestions(questionsRes.data || []);
      }
    } catch (err: any) {
      toast.error(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const toggleCluster = (clusterId: string) => {
    setExpanded(prev => {
      const newClusters = new Set(prev.clusters);
      if (newClusters.has(clusterId)) {
        newClusters.delete(clusterId);
      } else {
        newClusters.add(clusterId);
      }
      return { ...prev, clusters: newClusters };
    });
  };

  const toggleCompetency = (competencyId: string) => {
    setExpanded(prev => {
      const newCompetencies = new Set(prev.competencies);
      if (newCompetencies.has(competencyId)) {
        newCompetencies.delete(competencyId);
      } else {
        newCompetencies.add(competencyId);
      }
      return { ...prev, competencies: newCompetencies };
    });
  };

  // Cluster CRUD
  const handleAddCluster = () => {
    setIsAddingCluster(true);
    setClusterForm({ ClusterName: '', Description: '' });
  };

  const handleEditCluster = (cluster: ClusterListResponse) => {
    setEditingClusterId(cluster.Id);
    setClusterForm({ ClusterName: cluster.ClusterName, Description: cluster.Description || '' });
  };

  const handleSaveCluster = async () => {
    if (!token) {
      toast.error('Authentication required');
      return;
    }

    if (!clusterForm.ClusterName.trim()) {
      toast.error('Cluster name is required');
      return;
    }

    try {
      if (isAddingCluster) {
        const createData: CreateClusterRequest = {
          ClusterName: clusterForm.ClusterName,
          Description: clusterForm.Description || undefined,
        };
        const result = await questionService.createCluster(tenantSlug, createData, token);
        if (result.error) {
          toast.error(`Failed to create cluster: ${result.error}`);
        } else {
          toast.success('Cluster created successfully');
          setIsAddingCluster(false);
          loadAllData();
        }
      } else if (editingClusterId) {
        const updateData: UpdateClusterRequest = {
          ClusterName: clusterForm.ClusterName,
          Description: clusterForm.Description || undefined,
          IsActive: true,
        };
        const result = await questionService.updateCluster(tenantSlug, editingClusterId, updateData, token);
        if (result.error) {
          toast.error(`Failed to update cluster: ${result.error}`);
        } else {
          toast.success('Cluster updated successfully');
          setEditingClusterId(null);
          loadAllData();
        }
      }
    } catch (err: any) {
      toast.error(err.message || 'Failed to save cluster');
    }
  };

  const handleCancelCluster = () => {
    setIsAddingCluster(false);
    setEditingClusterId(null);
    setClusterForm({ ClusterName: '', Description: '' });
  };

  const handleDeleteCluster = async (clusterId: string) => {
    if (!token) {
      toast.error('Authentication required');
      return;
    }

    if (!confirm('⚠️ Are you sure you want to delete this cluster?\n\nThis will also delete all competencies and questions within it.')) {
      return;
    }

    const loadingToast = toast.loading('Deleting cluster...');
    try {
      const result = await questionService.deleteCluster(tenantSlug, clusterId, token);
      toast.dismiss(loadingToast);
      if (result.success) {
        toast.success('Cluster deleted successfully');
        loadAllData();
      } else {
        toast.error(`Failed to delete cluster: ${result.error || 'Unknown error'}`);
      }
    } catch (err: any) {
      toast.dismiss(loadingToast);
      toast.error(err.message || 'Failed to delete cluster');
    }
  };

  // Competency CRUD
  const handleAddCompetency = (clusterId: string) => {
    setAddingCompetencyToCluster(clusterId);
    setCompetencyForm({ Name: '', Description: '', ClusterId: clusterId });
    // Auto-expand cluster
    setExpanded(prev => ({ ...prev, clusters: new Set([...prev.clusters, clusterId]) }));
  };

  const handleEditCompetency = (competency: CompetencyListResponse) => {
    setEditingCompetencyId(competency.Id);
    setCompetencyForm({ Name: competency.Name, Description: competency.Description || '', ClusterId: competency.ClusterId });
  };

  const handleSaveCompetency = async () => {
    if (!token) {
      toast.error('Authentication required');
      return;
    }

    if (!competencyForm.Name.trim()) {
      toast.error('Competency name is required');
      return;
    }

    try {
      if (addingCompetencyToCluster) {
        const createData: CreateCompetencyRequest = {
          Name: competencyForm.Name,
          Description: competencyForm.Description || undefined,
          ClusterId: competencyForm.ClusterId,
        };
        const result = await questionService.createCompetency(tenantSlug, createData, token);
        if (result.error) {
          toast.error(`Failed to create competency: ${result.error}`);
        } else {
          toast.success(' Competency created successfully');
          setAddingCompetencyToCluster(null);
          loadAllData();
        }
      } else if (editingCompetencyId) {
        const updateData: UpdateCompetencyRequest = {
          Name: competencyForm.Name,
          Description: competencyForm.Description || undefined,
          ClusterId: competencyForm.ClusterId,
          IsActive: true,
        };
        const result = await questionService.updateCompetency(tenantSlug, editingCompetencyId, updateData, token);
        if (result.error) {
          toast.error(`Failed to update competency: ${result.error}`);
        } else {
          toast.success('Competency updated successfully');
          setEditingCompetencyId(null);
          loadAllData();
        }
      }
    } catch (err: any) {
      toast.error(err.message || 'Failed to save competency');
    }
  };

  const handleCancelCompetency = () => {
    setAddingCompetencyToCluster(null);
    setEditingCompetencyId(null);
    setCompetencyForm({ Name: '', Description: '', ClusterId: '' });
  };

  const handleDeleteCompetency = async (competencyId: string) => {
    if (!token) {
      toast.error('Authentication required');
      return;
    }

    if (!confirm('⚠️ Are you sure you want to delete this competency?\n\nThis will also delete all questions within it.')) {
      return;
    }

    const loadingToast = toast.loading('Deleting competency...');
    try {
      const result = await questionService.deleteCompetency(tenantSlug, competencyId, token);
      toast.dismiss(loadingToast);
      if (result.success) {
        toast.success('Competency deleted successfully');
        loadAllData();
      } else {
        toast.error(`Failed to delete competency: ${result.error || 'Unknown error'}`);
      }
    } catch (err: any) {
      toast.dismiss(loadingToast);
      toast.error(err.message || 'Failed to delete competency');
    }
  };

  // Question CRUD
  const handleAddQuestion = (competencyId: string) => {
    setAddingQuestionToCompetency(competencyId);
    setQuestionForm({ SelfQuestion: '', OthersQuestion: '', CompetencyId: competencyId });
    // Auto-expand competency
    setExpanded(prev => ({ ...prev, competencies: new Set([...prev.competencies, competencyId]) }));
  };

  const handleEditQuestion = (question: QuestionListResponse) => {
    setEditingQuestionId(question.Id);
    setQuestionForm({
      SelfQuestion: question.SelfQuestion,
      OthersQuestion: question.OthersQuestion,
      CompetencyId: question.CompetencyId
    });
  };

  const handleSaveQuestion = async () => {
    if (!token) {
      toast.error('Authentication required');
      return;
    }

    if (!questionForm.SelfQuestion.trim() || !questionForm.OthersQuestion.trim()) {
      toast.error('Both Self Question and Others Question are required');
      return;
    }

    try {
      if (addingQuestionToCompetency) {
        const createData: CreateQuestionRequest = {
          SelfQuestion: questionForm.SelfQuestion,
          OthersQuestion: questionForm.OthersQuestion,
          CompetencyId: questionForm.CompetencyId,
        };
        const result = await questionService.createQuestion(tenantSlug, createData, token);
        if (result.error) {
          toast.error(`Failed to create question: ${result.error}`);
        } else {
          toast.success('Question created successfully');
          setAddingQuestionToCompetency(null);
          loadAllData();
        }
      } else if (editingQuestionId) {
        const updateData: UpdateQuestionRequest = {
          SelfQuestion: questionForm.SelfQuestion,
          OthersQuestion: questionForm.OthersQuestion,
          CompetencyId: questionForm.CompetencyId,
          IsActive: true,
        };
        const result = await questionService.updateQuestion(tenantSlug, editingQuestionId, updateData, token);
        if (result.error) {
          toast.error(`Failed to update question: ${result.error}`);
        } else {
          toast.success('Question updated successfully');
          setEditingQuestionId(null);
          loadAllData();
        }
      }
    } catch (err: any) {
      toast.error(err.message || 'Failed to save question');
    }
  };

  const handleCancelQuestion = () => {
    setAddingQuestionToCompetency(null);
    setEditingQuestionId(null);
    setQuestionForm({ SelfQuestion: '', OthersQuestion: '', CompetencyId: '' });
  };

  const handleDeleteQuestion = async (questionId: string) => {
    if (!token) {
      toast.error('Authentication required');
      return;
    }

    if (!confirm('⚠️ Are you sure you want to delete this question?')) {
      return;
    }

    const loadingToast = toast.loading('Deleting question...');
    try {
      const result = await questionService.deleteQuestion(tenantSlug, questionId, token);
      toast.dismiss(loadingToast);
      if (result.success) {
        toast.success('Question deleted successfully');
        loadAllData();
      } else {
        toast.error(`Failed to delete question: ${result.error || 'Unknown error'}`);
      }
    } catch (err: any) {
      toast.dismiss(loadingToast);
      toast.error(err.message || 'Failed to delete question');
    }
  };

  const getCompetenciesForCluster = (clusterId: string) => {
    return competencies.filter(c => c.ClusterId === clusterId);
  };

  const getQuestionsForCompetency = (competencyId: string) => {
    return questions.filter(q => q.CompetencyId === competencyId);
  };

  if (loading) {
    return (
      <div className={styles.loading}>
        <div className={styles.spinner}></div>
      </div>
    );
  }

  return (
    <>
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 4000,
          style: {
            background: '#363636',
            color: '#fff',
          },
          success: {
            duration: 3000,
            style: {
              background: '#10B981',
            },
          },
          error: {
            duration: 5000,
            style: {
              background: '#EF4444',
            },
          },
        }}
      />
      <div className={styles.container}>
        <div className={styles.header}>
          <h1 className={styles.title}>Question Management</h1>
          <p className={styles.subtitle}>Manage clusters, competencies, and questions for your surveys</p>
        </div>

        <button onClick={handleAddCluster} className={styles.addClusterButton}>
          + Add Cluster
        </button>

      {/* Adding New Cluster Form */}
      {isAddingCluster && (
        <div className={styles.clusterCard}>
          <div className={styles.clusterEditForm}>
            <div className={styles.formGroup}>
              <label className={styles.label}>Cluster Name *</label>
              <input
                type="text"
                className={styles.input}
                value={clusterForm.ClusterName}
                onChange={(e) => setClusterForm({ ...clusterForm, ClusterName: e.target.value })}
                placeholder="Enter cluster name"
                autoFocus
              />
            </div>
            <div className={styles.formGroup}>
              <label className={styles.label}>Description</label>
              <textarea
                className={styles.textarea}
                value={clusterForm.Description}
                onChange={(e) => setClusterForm({ ...clusterForm, Description: e.target.value })}
                placeholder="Enter description (optional)"
                rows={2}
              />
            </div>
            <div className={styles.formActions}>
              <button onClick={handleSaveCluster} className={styles.saveButton}>Save</button>
              <button onClick={handleCancelCluster} className={styles.cancelButton}>Cancel</button>
            </div>
          </div>
        </div>
      )}

      {/* Clusters List */}
      {clusters.length === 0 && !isAddingCluster ? (
        <div className={styles.emptyState}>
          <h3>No Clusters Yet</h3>
          <p>Get started by creating your first cluster</p>
        </div>
      ) : (
        <div className={styles.clustersList}>
          {clusters.map((cluster) => {
            const isExpanded = expanded.clusters.has(cluster.Id);
            const isEditing = editingClusterId === cluster.Id;
            const clusterCompetencies = getCompetenciesForCluster(cluster.Id);

            return (
              <div key={cluster.Id} className={styles.clusterCard}>
                {/* Cluster Header */}
                {!isEditing ? (
                  <div className={styles.clusterHeader} onClick={() => toggleCluster(cluster.Id)}>
                    <svg className={`${styles.expandIcon} ${isExpanded ? styles.expanded : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                    </svg>
                    <div className={styles.clusterInfo}>
                      <h3 className={styles.clusterName}>{cluster.ClusterName}</h3>
                      <p className={styles.clusterMeta}>{cluster.CompetencyCount} competenc{cluster.CompetencyCount === 1 ? 'y' : 'ies'}</p>
                    </div>
                    <div className={styles.clusterActions} onClick={(e) => e.stopPropagation()}>
                      <button onClick={() => handleEditCluster(cluster)} className={styles.iconButton} title="Edit">
                        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                      </button>
                      <button onClick={() => handleDeleteCluster(cluster.Id)} className={`${styles.iconButton} ${styles.delete}`} title="Delete">
                        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    </div>
                  </div>
                ) : (
                  <div className={styles.clusterEditForm}>
                    <div className={styles.formGroup}>
                      <label className={styles.label}>Cluster Name *</label>
                      <input
                        type="text"
                        className={styles.input}
                        value={clusterForm.ClusterName}
                        onChange={(e) => setClusterForm({ ...clusterForm, ClusterName: e.target.value })}
                        autoFocus
                      />
                    </div>
                    <div className={styles.formGroup}>
                      <label className={styles.label}>Description</label>
                      <textarea
                        className={styles.textarea}
                        value={clusterForm.Description}
                        onChange={(e) => setClusterForm({ ...clusterForm, Description: e.target.value })}
                        rows={2}
                      />
                    </div>
                    <div className={styles.formActions}>
                      <button onClick={handleSaveCluster} className={styles.saveButton}>Save</button>
                      <button onClick={handleCancelCluster} className={styles.cancelButton}>Cancel</button>
                    </div>
                  </div>
                )}

                {/* Cluster Content - Competencies */}
                {isExpanded && !isEditing && (
                  <div className={styles.clusterContent}>
                    {/* Adding New Competency Form */}
                    {addingCompetencyToCluster === cluster.Id && (
                      <div className={styles.competencyCard}>
                        <div className={styles.clusterEditForm}>
                          <div className={styles.formGroup}>
                            <label className={styles.label}>Competency Name *</label>
                            <input
                              type="text"
                              className={styles.input}
                              value={competencyForm.Name}
                              onChange={(e) => setCompetencyForm({ ...competencyForm, Name: e.target.value })}
                              placeholder="Enter competency name"
                              autoFocus
                            />
                          </div>
                          <div className={styles.formGroup}>
                            <label className={styles.label}>Description</label>
                            <textarea
                              className={styles.textarea}
                              value={competencyForm.Description}
                              onChange={(e) => setCompetencyForm({ ...competencyForm, Description: e.target.value })}
                              placeholder="Enter description (optional)"
                              rows={2}
                            />
                          </div>
                          <div className={styles.formActions}>
                            <button onClick={handleSaveCompetency} className={styles.saveButton}>Save</button>
                            <button onClick={handleCancelCompetency} className={styles.cancelButton}>Cancel</button>
                          </div>
                        </div>
                      </div>
                    )}

                    {/* Competencies List */}
                    <div className={styles.competenciesList}>
                      {clusterCompetencies.map((competency) => {
                        const isCompExpanded = expanded.competencies.has(competency.Id);
                        const isCompEditing = editingCompetencyId === competency.Id;
                        const competencyQuestions = getQuestionsForCompetency(competency.Id);

                        return (
                          <div key={competency.Id} className={styles.competencyCard}>
                            {/* Competency Header */}
                            {!isCompEditing ? (
                              <div className={styles.competencyHeader} onClick={() => toggleCompetency(competency.Id)}>
                                <svg className={`${styles.expandIcon} ${isCompExpanded ? styles.expanded : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                                </svg>
                                <div className={styles.competencyInfo}>
                                  <h4 className={styles.competencyName}>{competency.Name}</h4>
                                  <p className={styles.competencyMeta}>{competency.QuestionCount} question{competency.QuestionCount === 1 ? '' : 's'}</p>
                                </div>
                                <div className={styles.clusterActions} onClick={(e) => e.stopPropagation()}>
                                  <button onClick={() => handleEditCompetency(competency)} className={styles.iconButton} title="Edit">
                                    <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                                    </svg>
                                  </button>
                                  <button onClick={() => handleDeleteCompetency(competency.Id)} className={`${styles.iconButton} ${styles.delete}`} title="Delete">
                                    <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                    </svg>
                                  </button>
                                </div>
                              </div>
                            ) : (
                              <div className={styles.clusterEditForm}>
                                <div className={styles.formGroup}>
                                  <label className={styles.label}>Competency Name *</label>
                                  <input
                                    type="text"
                                    className={styles.input}
                                    value={competencyForm.Name}
                                    onChange={(e) => setCompetencyForm({ ...competencyForm, Name: e.target.value })}
                                    autoFocus
                                  />
                                </div>
                                <div className={styles.formGroup}>
                                  <label className={styles.label}>Description</label>
                                  <textarea
                                    className={styles.textarea}
                                    value={competencyForm.Description}
                                    onChange={(e) => setCompetencyForm({ ...competencyForm, Description: e.target.value })}
                                    rows={2}
                                  />
                                </div>
                                <div className={styles.formActions}>
                                  <button onClick={handleSaveCompetency} className={styles.saveButton}>Save</button>
                                  <button onClick={handleCancelCompetency} className={styles.cancelButton}>Cancel</button>
                                </div>
                              </div>
                            )}

                            {/* Competency Content - Questions */}
                            {isCompExpanded && !isCompEditing && (
                              <div className={styles.competencyContent}>
                                {/* Adding New Question Form */}
                                {addingQuestionToCompetency === competency.Id && (
                                  <div className={styles.questionCard}>
                                    <div className={styles.formGroup}>
                                      <label className={styles.label}>Self Question *</label>
                                      <textarea
                                        className={styles.textarea}
                                        value={questionForm.SelfQuestion}
                                        onChange={(e) => setQuestionForm({ ...questionForm, SelfQuestion: e.target.value })}
                                        placeholder="Enter the question for self-evaluation"
                                        rows={2}
                                        autoFocus
                                      />
                                    </div>
                                    <div className={styles.formGroup}>
                                      <label className={styles.label}>Others Question *</label>
                                      <textarea
                                        className={styles.textarea}
                                        value={questionForm.OthersQuestion}
                                        onChange={(e) => setQuestionForm({ ...questionForm, OthersQuestion: e.target.value })}
                                        placeholder="Enter the question for peer/manager evaluation"
                                        rows={2}
                                      />
                                    </div>
                                    <div className={styles.formActions}>
                                      <button onClick={handleSaveQuestion} className={styles.saveButton}>Save</button>
                                      <button onClick={handleCancelQuestion} className={styles.cancelButton}>Cancel</button>
                                    </div>
                                  </div>
                                )}

                                {/* Questions List */}
                                <div className={styles.questionsList}>
                                  {competencyQuestions.map((question) => {
                                    const isQEditing = editingQuestionId === question.Id;

                                    return (
                                      <div key={question.Id} className={styles.questionCard}>
                                        {!isQEditing ? (
                                          <div className={styles.questionHeader}>
                                            <div className={styles.questionContent}>
                                              <div className={styles.questionGrid}>
                                                <div>
                                                  <p className={styles.questionLabel}>Self Question</p>
                                                  <p className={styles.questionText}>{question.SelfQuestion}</p>
                                                </div>
                                                <div>
                                                  <p className={styles.questionLabel}>Others Question</p>
                                                  <p className={styles.questionText}>{question.OthersQuestion}</p>
                                                </div>
                                              </div>
                                            </div>
                                            <div className={styles.clusterActions}>
                                              <button onClick={() => handleEditQuestion(question)} className={styles.iconButton} title="Edit">
                                                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                                                </svg>
                                              </button>
                                              <button onClick={() => handleDeleteQuestion(question.Id)} className={`${styles.iconButton} ${styles.delete}`} title="Delete">
                                                <svg fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                                </svg>
                                              </button>
                                            </div>
                                          </div>
                                        ) : (
                                          <div>
                                            <div className={styles.formGroup}>
                                              <label className={styles.label}>Self Question *</label>
                                              <textarea
                                                className={styles.textarea}
                                                value={questionForm.SelfQuestion}
                                                onChange={(e) => setQuestionForm({ ...questionForm, SelfQuestion: e.target.value })}
                                                rows={2}
                                                autoFocus
                                              />
                                            </div>
                                            <div className={styles.formGroup}>
                                              <label className={styles.label}>Others Question *</label>
                                              <textarea
                                                className={styles.textarea}
                                                value={questionForm.OthersQuestion}
                                                onChange={(e) => setQuestionForm({ ...questionForm, OthersQuestion: e.target.value })}
                                                rows={2}
                                              />
                                            </div>
                                            <div className={styles.formActions}>
                                              <button onClick={handleSaveQuestion} className={styles.saveButton}>Save</button>
                                              <button onClick={handleCancelQuestion} className={styles.cancelButton}>Cancel</button>
                                            </div>
                                          </div>
                                        )}
                                      </div>
                                    );
                                  })}
                                </div>

                                {/* Add Question Button */}
                                {!addingQuestionToCompetency && (
                                  <button onClick={() => handleAddQuestion(competency.Id)} className={styles.addQuestionButton}>
                                    + Add Question
                                  </button>
                                )}
                              </div>
                            )}
                          </div>
                        );
                      })}
                    </div>

                    {/* Add Competency Button */}
                    {!addingCompetencyToCluster && (
                      <button onClick={() => handleAddCompetency(cluster.Id)} className={styles.addCompetencyButton}>
                        + Add Competency
                      </button>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
      </div>
    </>
  );
}

