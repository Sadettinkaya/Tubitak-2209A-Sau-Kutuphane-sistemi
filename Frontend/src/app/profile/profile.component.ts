import { Component, OnInit, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReservationService } from '../services/reservation.service';
import { AuthService } from '../services/auth.service';
import { FeedbackService } from '../services/feedback.service';
import { Router } from '@angular/router';
import { PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container mt-5">
      <div class="card">
        <div class="card-header bg-primary text-white">
          <h3>Profilim</h3>
        </div>
        <div class="card-body">
          <div class="row mb-4">
            <div class="col-md-12">
              <h5>Öğrenci Bilgileri</h5>
              <p><strong>Öğrenci Numarası:</strong> {{ studentNumber }}</p>
              <p *ngIf="academicLevel"><strong>Akademik Seviye (Kayıtlı):</strong> {{ academicLevel }}</p>
              <div *ngIf="isLoadingProfile" class="alert alert-secondary mt-3">Ceza durumu yükleniyor...</div>
              <div *ngIf="!isLoadingProfile && profileError" class="alert alert-warning mt-3">{{ profileError }}</div>
              <div *ngIf="!isLoadingProfile && penaltyInfo" class="alert" [ngClass]="penaltyInfo.banUntil ? 'alert-danger' : 'alert-info'">
                <div><strong>Sistem Kaydı:</strong> {{ penaltyInfo.studentType }}</div>
                <div><strong>Ceza Puanı:</strong> {{ penaltyInfo.penaltyPoints }}</div>
                <div><strong>Son İşlenen No-Show:</strong> {{ penaltyInfo.lastNoShowProcessedAt | date:'short' }}</div>
                <div *ngIf="penaltyInfo.banUntil"><strong>Ban Bitiş Tarihi:</strong> {{ penaltyInfo.banUntil }}</div>
                <div *ngIf="penaltyInfo.banReason" class="mt-2"><strong>Ceza Nedeni:</strong> {{ penaltyInfo.banReason }}</div>
              </div>
            </div>
          </div>

          <div class="row">
            <div class="col-md-12">
              <h5>Rezervasyonlarım</h5>
              <div *ngIf="isLoadingReservations" class="alert alert-secondary">
                Rezervasyonlar yükleniyor...
              </div>
              <div *ngIf="!isLoadingReservations && reservations.length === 0" class="alert alert-info">
                Henüz bir rezervasyonunuz bulunmamaktadır.
              </div>
              <table *ngIf="!isLoadingReservations && reservations.length > 0" class="table table-striped">
                <thead>
                  <tr>
                    <th>Tarih</th>
                    <th>Saat</th>
                    <th>Masa</th>
                    <th>Durum</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let res of reservations">
                    <td>{{ res.reservationDate || res.ReservationDate }}</td>
                    <td>{{ res.startTime || res.StartTime }} - {{ res.endTime || res.EndTime }}</td>
                    <td>{{ res.tableNumber || res.TableNumber }} (Kat: {{ res.floorId || res.FloorId }})</td>
                    <td>
                      <span *ngIf="res.isAttended || res.IsAttended" class="badge bg-success">Giriş Yapıldı</span>
                      <span *ngIf="!(res.isAttended || res.IsAttended)" class="badge bg-warning text-dark">Bekliyor</span>
                    </td>
                    <td>
                      <button *ngIf="!(res.isAttended || res.IsAttended)" class="btn btn-danger btn-sm" (click)="cancelReservation(res.id || res.Id)">İptal Et</button>
                    </td>
                  </tr>
                </tbody>
              </table>
              <div class="mt-4">
                <h5>Geri Bildirimlerim</h5>
                <div *ngIf="isLoadingFeedbacks" class="alert alert-secondary">
                  Geri bildirimler yükleniyor...
                </div>
                <table *ngIf="!isLoadingFeedbacks && feedbacks.length > 0" class="table table-bordered">
                  <thead>
                    <tr>
                      <th>Tarih</th>
                      <th>Mesaj</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let fb of feedbacks">
                      <td>{{ fb.date || fb.Date | date:'short' }}</td>
                      <td>{{ fb.message || fb.Message }}</td>
                    </tr>
                  </tbody>
                </table>
                <div *ngIf="!isLoadingFeedbacks && feedbacks.length === 0" class="alert alert-info">Henüz bir geri bildiriminiz yok.</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: []
})
export class ProfileComponent implements OnInit {
  studentNumber: string | null = '';
  academicLevel: string | null = null;
  reservations: any[] = [];
  feedbacks: any[] = [];
  penaltyInfo: any = null;
  isLoadingReservations = false;
  isLoadingFeedbacks = false;
  isLoadingProfile = false;
  profileError = '';

  constructor(
    private reservationService: ReservationService,
    private authService: AuthService,
    private feedbackService: FeedbackService,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    this.studentNumber = this.authService.getCurrentUser();
    this.academicLevel = this.authService.getAcademicLevel();
    if (!this.studentNumber) {
      this.router.navigate(['/login']);
      return;
    }
    this.loadReservations();
    this.loadFeedbacks();
    this.loadProfileInfo();
  }

  loadReservations() {
    if (this.studentNumber) {
      this.isLoadingReservations = true;
      this.reservationService.getMyReservations(this.studentNumber).subscribe({
        next: (data) => {
          console.log('Reservations received:', data);
          if (Array.isArray(data)) {
             this.reservations = data;
          } else {
             console.error('Data is not an array:', data);
             this.reservations = [];
          }
          this.isLoadingReservations = false;
        },
        error: (err) => {
          console.error('Error loading reservations:', err);
          this.reservations = [];
          this.isLoadingReservations = false;
        }
      });
    }
  }

  loadProfileInfo() {
    if (!this.studentNumber) {
      return;
    }

    this.isLoadingProfile = true;
    this.profileError = '';

    this.reservationService.getStudentProfile(this.studentNumber).subscribe({
      next: (data) => {
        this.penaltyInfo = data;
        this.isLoadingProfile = false;
      },
      error: (err) => {
        this.profileError = 'Ceza bilgileri alınamadı.';
        this.penaltyInfo = null;
        this.isLoadingProfile = false;
      }
    });
  }

  loadFeedbacks() {
    if (this.studentNumber) {
      this.isLoadingFeedbacks = true;
      this.feedbackService.getFeedbacks().subscribe({
        next: (data) => {
          if (Array.isArray(data)) {
            this.feedbacks = data.filter(fb => (fb.studentNumber || fb.StudentNumber) === this.studentNumber);
          } else {
            this.feedbacks = [];
          }
          this.isLoadingFeedbacks = false;
        },
        error: (err) => {
          this.feedbacks = [];
          this.isLoadingFeedbacks = false;
        }
      });
    }
  }

  cancelReservation(id: number) {
    if (confirm('Bu rezervasyonu iptal etmek istediğinize emin misiniz?')) {
      this.reservationService.cancelReservation(id).subscribe({
        next: () => {
          alert('Rezervasyon iptal edildi.');
          this.reservations = this.reservations.filter(res => (res.id || res.Id) !== id);
          if (this.reservations.length === 0) {
            this.loadReservations();
          }
        },
        error: (err) => alert('İptal işlemi başarısız.')
      });
    }
  }
}
