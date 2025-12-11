import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { finalize } from 'rxjs';
import { ReservationService } from '../services/reservation.service';

@Component({
  selector: 'app-turnstile',
  templateUrl: './turnstile.component.html',
  styleUrls: ['./turnstile.component.css'],
  standalone: false
})
export class TurnstileComponent implements OnInit {
  studentNumber: string = '';
  message: string = '';
  isSuccess: boolean = false;
  isLoading: boolean = false;

  constructor(
    private reservationService: ReservationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    // Her sayfa ziyaretinde state'i temizle
    this.studentNumber = '';
    this.message = '';
    this.isSuccess = false;
    this.isLoading = false;
  }

  onEnter() {
    if (this.isLoading) {
      return; // double-click engelle
    }

    if (!this.studentNumber) {
      alert('Lütfen öğrenci numaranızı giriniz.');
      return;
    }

    this.message = '';
    this.isSuccess = false;
    this.isLoading = true;
    this.cdr.detectChanges(); // UI'ı hemen güncelle

    this.reservationService
      .enterTurnstile(this.studentNumber)
      .pipe(finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges(); // Loading bitince UI'ı güncelle
      }))
      .subscribe({
        next: (response) => {
          this.isSuccess = !!response?.doorOpen;
          this.message = response?.message || (this.isSuccess ? 'Giriş onaylandı.' : 'Giriş reddedildi.');
          if (this.isSuccess) {
            this.studentNumber = '';
          }
          this.cdr.detectChanges(); // Sonucu göster
        },
        error: (err) => {
          this.isSuccess = false;
          this.message = err?.error?.message || 'Servise ulaşılamadı veya giriş başarısız.';
          this.cdr.detectChanges(); // Hatayı göster
        }
      });
  }
}
