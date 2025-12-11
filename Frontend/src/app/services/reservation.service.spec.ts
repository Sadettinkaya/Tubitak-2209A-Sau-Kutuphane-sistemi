import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Table } from '../models/table';

@Injectable({
  providedIn: 'root'
})
export class ReservationService {

  private apiUrl = 'https://localhost:7234/api/ReservationApi';  

  constructor(private http: HttpClient) { }

  getAvailableTables(date: string, startTime: string, endTime: string, floorId: number): Observable<Table[]> {
    const params = new HttpParams()
      .set('date', date)
      .set('start', startTime)
      .set('end', endTime)
      .set('floorId', floorId.toString());

    return this.http.get<Table[]>(`${this.apiUrl}/Tables`, { params });
  }
}


