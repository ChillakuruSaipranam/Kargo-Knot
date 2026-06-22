export interface TruckRepair {
  id: string;
  date: string;
  truckNumber: string;
  description: string;
  cost: number;
  createdBy: string;
  createdAt: string;
}

export interface RepairFilter {
  dateFrom: string;
  dateTo: string;
  truckNumber: string;
  minCost: number | null;
  maxCost: number | null;
}

export interface RepairSummary {
  totalRepairs: number;
  totalCost: number;
}

export const emptyRepairFilter = (): RepairFilter => ({
  dateFrom: '',
  dateTo: '',
  truckNumber: '',
  minCost: null,
  maxCost: null,
});
