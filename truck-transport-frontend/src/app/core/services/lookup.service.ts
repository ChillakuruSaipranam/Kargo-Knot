import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LookupItem } from '../models/lookup.model';

@Injectable({ providedIn: 'root' })
export class LookupService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/lookups`;

  getTrucks(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/trucks`);
  }

  createTruck(number: string): Observable<LookupItem> {
    return this.http.post<LookupItem>(`${this.baseUrl}/trucks`, { number });
  }

  getQuarries(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/quarries`);
  }

  createQuarry(name: string): Observable<LookupItem> {
    return this.http.post<LookupItem>(`${this.baseUrl}/quarries`, { name });
  }

  getDrivers(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/drivers`);
  }

  createDriver(name: string, phone: string, license: string): Observable<LookupItem> {
    return this.http.post<LookupItem>(`${this.baseUrl}/drivers`, { name, phone, license });
  }
}
