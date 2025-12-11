import { Component } from '@angular/core';
import { finalize } from 'rxjs';
import { ReservationService } from '../services/reservation.service';

@Component({
  selector: 'app-turnstile',
  templateUrl: './turnstile.component.html',
  styleUrls: ['./turnstile.component.css'],
  standalone: false
})
export class TurnstileComponent {
  studentNumber: string = '';
  message: string = '';
  isSuccess: boolean = false;
  isLoading: boolean = false;

  constructor(private reservationService: ReservationService) {}

  onEnter() {
    if (!this.studentNumber) {
      alert('Lütfen öğrenci numaranızı giriniz.');
      return;
    }

    this.isLoading = true;
    this.reservationService
      .enterTurnstile(this.studentNumber)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
      next: (response) => {
        this.isSuccess = response.doorOpen;
        this.message = response.message;
        if (this.isSuccess) {
          this.studentNumber = '';
        }
      },
      error: (err) => {
        this.isSuccess = false;
        this.message = err.error?.message || 'Giriş başarısız.';
      }
      });
  }
}
