import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Table } from '../models/table';

@Injectable({
  providedIn: 'root'
})

export class ReservationService {

  private apiUrl = 'http://localhost:5010/api/Reservation';
  //constructor(private reservationService: ReservationService) { }
  constructor(private http: HttpClient) { }


  getTables(date: string, startTime: string, endTime: string, floorId: number): Observable<any[]> {
  const params = new HttpParams()
    .set('date', date)
    .set('start', startTime)
    .set('end', endTime)
    .set('floorId', floorId.toString());

  return this.http.get<any[]>(`${this.apiUrl}/Tables`, { params });
}

  createReservation(reservation: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Create`, reservation);
  }

  getMyReservations(studentNumber: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/MyReservations?studentNumber=${studentNumber}`);
  }

  getStudentProfile(studentNumber: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/Profile/${studentNumber}`);
  }

  getPenaltyList(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/Penalties`);
  }

  getAllReservations(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/All`);
  }

  cancelReservation(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/Cancel/${id}`);
  }

  enterTurnstile(studentNumber: string): Observable<any> {
    // Turnstile Service is on port 5003, but via Gateway it is /api/Turnstile
    // The Gateway is on port 5010.
    // My ReservationService apiUrl is http://localhost:5010/api/Reservation
    // So I need to change the base url for this call.
    return this.http.post('http://localhost:5010/api/Turnstile/Enter', { studentNumber });
  }

}
