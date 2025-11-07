// Types for Cluster, Competency, and Question entities

// Cluster Types
export interface Cluster {
  Id: string;
  ClusterName: string;
  Description?: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  Competencies?: Competency[];
}

export interface ClusterResponse {
  Id: string;
  ClusterName: string;
  Description?: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  Competencies: CompetencyResponse[];
}

export interface ClusterListResponse {
  Id: string;
  ClusterName: string;
  Description?: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  CompetencyCount: number;
}

export interface CreateClusterRequest {
  ClusterName: string;
  Description?: string;
}

export interface UpdateClusterRequest {
  ClusterName: string;
  Description?: string;
  IsActive?: boolean;
}

// Competency Types
export interface Competency {
  Id: string;
  Name: string;
  Description?: string;
  ClusterId: string;
  ClusterName?: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  Questions?: Question[];
}

export interface CompetencyResponse {
  Id: string;
  Name: string;
  Description?: string;
  ClusterId: string;
  ClusterName: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  Questions: QuestionResponse[];
}

export interface CompetencyListResponse {
  Id: string;
  Name: string;
  Description?: string;
  ClusterId: string;
  ClusterName: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  QuestionCount: number;
}

export interface CreateCompetencyRequest {
  Name: string;
  Description?: string;
  ClusterId: string;
}

export interface UpdateCompetencyRequest {
  Name: string;
  Description?: string;
  ClusterId: string;
  IsActive?: boolean;
}

// Question Types
export interface Question {
  Id: string;
  CompetencyId: string;
  CompetencyName?: string;
  SelfQuestion: string;
  OthersQuestion: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
}

export interface QuestionResponse {
  Id: string;
  CompetencyId: string;
  CompetencyName: string;
  SelfQuestion: string;
  OthersQuestion: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
}

export interface QuestionListResponse {
  Id: string;
  CompetencyId: string;
  CompetencyName: string;
  SelfQuestion: string;
  OthersQuestion: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
}

export interface CreateQuestionRequest {
  CompetencyId: string;
  SelfQuestion: string;
  OthersQuestion: string;
}

export interface UpdateQuestionRequest {
  CompetencyId: string;
  SelfQuestion: string;
  OthersQuestion: string;
  IsActive?: boolean;
}

