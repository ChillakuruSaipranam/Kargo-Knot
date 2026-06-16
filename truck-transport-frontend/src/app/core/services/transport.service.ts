import { Injectable, signal } from '@angular/core';
import { TransportTrip, TripFilter } from '../models/transport-trip.model';

const STORAGE_KEY = 'transport_trips';

@Injectable({ providedIn: 'root' })
export class TransportService {
  private readonly trips = signal<TransportTrip[]>(this.loadTrips());

  readonly tripsSignal = this.trips.asReadonly();

  getAll(): TransportTrip[] {
    return this.trips();
  }

  getById(id: string): TransportTrip | undefined {
    return this.trips().find((trip) => trip.id === id);
  }

  add(trip: Omit<TransportTrip, 'id' | 'createdAt'>): TransportTrip {
    const newTrip: TransportTrip = {
      ...trip,
      id: crypto.randomUUID(),
      createdAt: new Date().toISOString(),
    };
    this.trips.update((items) => [newTrip, ...items]);
    this.persist();
    return newTrip;
  }

  update(id: string, changes: Partial<TransportTrip>): TransportTrip | undefined {
    let updated: TransportTrip | undefined;
    this.trips.update((items) =>
      items.map((trip) => {
        if (trip.id !== id) {
          return trip;
        }
        updated = { ...trip, ...changes, id: trip.id };
        return updated;
      })
    );
    this.persist();
    return updated;
  }

  delete(id: string): void {
    this.trips.update((items) => items.filter((trip) => trip.id !== id));
    this.persist();
  }

  filter(filters: TripFilter): TransportTrip[] {
    return this.trips().filter((trip) => {
      if (filters.dateFrom && trip.date < filters.dateFrom) return false;
      if (filters.dateTo && trip.date > filters.dateTo) return false;
      if (filters.shift && trip.shift !== filters.shift) return false;
      if (filters.truckNumber && !trip.truckNumber.toLowerCase().includes(filters.truckNumber.toLowerCase())) {
        return false;
      }
      if (filters.quarryName && !trip.quarryName.toLowerCase().includes(filters.quarryName.toLowerCase())) {
        return false;
      }
      if (filters.driverName && !trip.driverName.toLowerCase().includes(filters.driverName.toLowerCase())) {
        return false;
      }
      if (filters.minTonnes !== null && trip.tonnes < filters.minTonnes) return false;
      if (filters.maxTonnes !== null && trip.tonnes > filters.maxTonnes) return false;
      return true;
    });
  }

  getSummary() {
    const items = this.trips();
    return {
      totalTrips: items.length,
      totalTonnes: items.reduce((sum, trip) => sum + trip.tonnes, 0),
      dayTrips: items.filter((trip) => trip.shift === 'Day').length,
      nightTrips: items.filter((trip) => trip.shift === 'Night').length,
    };
  }

  private loadTrips(): TransportTrip[] {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return this.seedData();
    }
    try {
      return JSON.parse(raw) as TransportTrip[];
    } catch {
      return this.seedData();
    }
  }

  private persist(): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.trips()));
  }

  private seedData(): TransportTrip[] {
    const seed: TransportTrip[] = [
      {
        id: crypto.randomUUID(),
        date: '2026-06-12',
        shift: 'Day',
        truckNumber: 'MH-12-AB-4521',
        quarryName: 'Shivam Stone Crusher #3',
        tonnes: 18.5,
        driverName: 'Rajesh Kumar',
        driverPhone: '+91 98765 43210',
        driverLicense: 'MH-2024-88912',
        startTime: '06:30',
        endTime: '14:15',
        createdAt: new Date().toISOString(),
        createdBy: 'admin@transport.com',
      },
      {
        id: crypto.randomUUID(),
        date: '2026-06-12',
        shift: 'Night',
        truckNumber: 'MH-12-CD-7788',
        quarryName: 'Blue Ridge Quarry',
        tonnes: 22,
        driverName: 'Suresh Patil',
        driverPhone: '+91 91234 56789',
        driverLicense: 'MH-2023-44102',
        startTime: '20:00',
        endTime: '04:30',
        createdAt: new Date().toISOString(),
        createdBy: 'user@transport.com',
      },
      {
        id: crypto.randomUUID(),
        date: '2026-06-13',
        shift: 'Day',
        truckNumber: 'MH-14-EF-3301',
        quarryName: 'Granite Hills Crusher #1',
        tonnes: 15.75,
        driverName: 'Amit Deshmukh',
        driverPhone: '+91 99887 76655',
        driverLicense: 'MH-2025-10234',
        startTime: '07:00',
        endTime: '15:45',
        createdAt: new Date().toISOString(),
        createdBy: 'user@transport.com',
      },
    ];
    localStorage.setItem(STORAGE_KEY, JSON.stringify(seed));
    return seed;
  }
}
