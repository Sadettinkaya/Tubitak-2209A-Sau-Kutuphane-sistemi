import { Component, OnInit, Inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FeedbackService } from '../services/feedback.service';
import { ReservationService } from '../services/reservation.service';
import { PLATFORM_ID } from '@angular/core';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-panel.component.html',
  styleUrls: ['./admin-panel.component.css']
})
export class AdminPanelComponent implements OnInit {
  feedbacks: any[] = [];
  reservations: any[] = [];
  penalties: any[] = [];
  isLoadingFeedbacks = false;
  isLoadingReservations = false;
  isLoadingPenalties = false;
  penaltiesError = '';

  constructor(
    private feedbackService: FeedbackService,
    private reservationService: ReservationService,
    private cdr: ChangeDetectorRef,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    this.loadData();
  }

  loadData() {
    this.isLoadingFeedbacks = true;
    this.isLoadingReservations = true;
    this.isLoadingPenalties = true;
    this.penaltiesError = '';
    this.cdr.detectChanges();

    this.feedbackService.getFeedbacks().subscribe({
      next: (data) => {
        this.feedbacks = Array.isArray(data) ? data : [];
        this.isLoadingFeedbacks = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.feedbacks = [];
        this.isLoadingFeedbacks = false;
        this.cdr.detectChanges();
      }
    });

    this.reservationService.getAllReservations().subscribe({
      next: (data) => {
        this.reservations = Array.isArray(data) ? data : [];
        this.isLoadingReservations = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.reservations = [];
        this.isLoadingReservations = false;
        this.cdr.detectChanges();
      }
    });

    this.reservationService.getPenaltyList().subscribe({
      next: (data) => {
        this.penalties = Array.isArray(data) ? data : [];
        this.isLoadingPenalties = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.penalties = [];
        this.penaltiesError = 'Ceza listesi alınamadı.';
        this.isLoadingPenalties = false;
        this.cdr.detectChanges();
      }
    });
  }
}
