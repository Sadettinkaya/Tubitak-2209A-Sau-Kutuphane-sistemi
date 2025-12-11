import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FeedbackService {
  private apiUrl = 'http://localhost:5010/api/Feedback';

  constructor(private http: HttpClient) { }

  submitFeedback(feedback: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/Submit`, feedback);
  }

  getFeedbacks(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  getAnalysis(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/Analysis`);
  }
}
