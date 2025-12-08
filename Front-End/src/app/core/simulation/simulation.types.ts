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

export interface SimulationInput {
  product: string;
  rateTable: string;
  rate: number;
  term: number;
  requestedAmount: number;
  financeIOF: boolean;
  gracePeriodDays: number;
  frequencyDays: number;
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
