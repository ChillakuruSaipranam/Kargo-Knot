export type ShiftType = 'Day' | 'Night';

export interface TransportTrip {
  id: string;
  date: string;
  shift: ShiftType;
  truckNumber: string;
  quarryName: string;
  numberOfTrips: number;
  tonnes: number;
  dieselLiters: number;
  driverName: string;
  driverPhone: string;
  driverLicense: string;
  startTime: string;
  endTime: string;
  createdAt: string;
  createdBy: string;
}

export interface TripFilter {
  dateFrom: string;
  dateTo: string;
  shift: ShiftType | '';
  truckNumber: string;
  quarryName: string;
  driverName: string;
  minTonnes: number | null;
  maxTonnes: number | null;
}

export const emptyTripFilter = (): TripFilter => ({
  dateFrom: '',
  dateTo: '',
  shift: '',
  truckNumber: '',
  quarryName: '',
  driverName: '',
  minTonnes: null,
  maxTonnes: null,
});
