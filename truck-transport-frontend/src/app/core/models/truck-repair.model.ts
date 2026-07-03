export interface TruckRepair {
  id: string;
  date: string;
  truckNumber: string;
  description: string;
  cost: number;
  driverName: string | null;
  createdBy: string;
  createdAt: string;
}

export interface RepairFilter {
  dateFrom: string;
  dateTo: string;
  truckNumber: string;
  driverName: string;
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
  driverName: '',
  minCost: null,
  maxCost: null,
});
