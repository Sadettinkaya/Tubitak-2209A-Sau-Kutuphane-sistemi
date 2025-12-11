import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly USER_KEY = 'current_user';
  private apiUrl = 'http://localhost:5010/api/Auth';

  constructor(private http: HttpClient) { }

  login(studentNumber: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, { studentNumber, password }).pipe(
      tap((response: any) => {
        if (response && response.studentNumber) {
          localStorage.setItem(this.USER_KEY, response.studentNumber);
          if (response.role) {
            localStorage.setItem('user_role', response.role);
          }
          if (response.academicLevel) {
            localStorage.setItem('academic_level', response.academicLevel);
          }
        }
      })
    );
  }

  register(user: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, user);
  }

  logout(): void {
    localStorage.removeItem(this.USER_KEY);
    localStorage.removeItem('user_role');
    localStorage.removeItem('academic_level');
  }

  getCurrentUser(): string | null {
    if (typeof localStorage !== 'undefined') {
      return localStorage.getItem(this.USER_KEY);
    }
    return null;
  }

  getUserRole(): string | null {
    if (typeof localStorage !== 'undefined') {
      return localStorage.getItem('user_role');
    }
    return null;
  }

  getAcademicLevel(): string | null {
    if (typeof localStorage !== 'undefined') {
      return localStorage.getItem('academic_level');
    }
    return null;
  }

  isAdmin(): boolean {
    return this.getUserRole() === 'admin';
  }

  isLoggedIn(): boolean {
    return !!this.getCurrentUser();
  }

  // JWT kaldırıldığı için şu an gerçek bir token kullanılmıyor;
  // interceptor derleme hatası vermesin diye boş bir getter bırakıyoruz.
  getToken(): string | null {
    return null;
  }
}
