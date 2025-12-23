import { PagedResult } from "app/core/user/user.service";

export interface RateTable {
  id: number;
  name: string;
  minInstallmentAmount: number;
  maxInstallmentAmount: number;
  availableRates: number[];
  availableTerms: number[];
  isActive: boolean;
}

export interface Simulation {
    id: number;
    product: string;
    rateTable: string;
    rate: number;
    term: number;
    installmentAmount: number;
    releasedAmount: number;
    contractValue: number;
    totalFinancedAmount: number;
    iofFinanced: boolean;
    hasGracePeriod: boolean;
    gracePeriodDays: number;
    frequencyDays: number;
    totalIOF: number;
    
    includeInsurance: boolean;
    insuranceRate: number;
    insuranceAmount: number;
    tacAmount: number;
    tacFinanced: boolean;

    refinancedFromId?: number;
    refinancedToId?: number;
    payoffAmount?: number;

    simulationDate: string;
    createdAt: string;
    createdBy: string;
}

export interface SimulationInput {
  product: string;
  rateTable: string;
  rate: number;
  term: number;
  requestedAmount: number;
  financeIOF: boolean;
  gracePeriodDays: number;
  frequencyDays: number;

  includeInsurance?: boolean;
  insuranceRate?: number;
  tacAmount?: number;
  financeTac?: boolean;

  refinancedFromId?: number;
}

export interface SimulationResult {
  releasedAmount: number;
  totalIOF: number;
  gracePeriodInterest: number;
  totalFinancedAmount: number;
  contractValue: number;
  installmentAmount: number;

  product: string;
  rateTable: string;
  rate: number;
  term: number;
  iofFinanced: boolean;
  gracePeriodDays: number;
  firstDueDate: string;

  includeInsurance: boolean;
  insuranceAmount: number;
  insuranceRate: number;
  tacAmount: number;
  tacFinanced: boolean;

  refinancedFromId?: number;
  payoffAmount?: number;

  installments: any[];
}

export interface Simulation extends SimulationResult {
  id: number;
  simulationDate: string;
  createdAt: string;
  createdBy: string;
}

export interface SimulationListItem {
  id: number;
  product: string;
  releasedAmount: number;
  installmentAmount: number;
  contractValue: number;
  createdAt: string;
  createdBy: string;
}

export interface DashboardTotals {
  totalSimulations: number;
  totalReleasedAmount: number;
  totalContractValue: number;
}

export interface PagedSimulationResult {
  items: SimulationListItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
