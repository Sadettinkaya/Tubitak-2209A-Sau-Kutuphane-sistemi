import { Component } from '@angular/core';
import { ReservationService } from '../services/reservation.service';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';
import { finalize, timeout } from 'rxjs';

@Component({
  selector: 'app-reservation-filter',
  templateUrl: './reservation-filter.component.html',
  standalone: false,
  styleUrls: ['./reservation-filter.component.css']
})
export class ReservationFilterComponent {
  filter = {
    date: '',
    startTime: '',
    endTime: '',
    floorId: 1
  };

  tables: any[] = [];
  lastErrorMessage = '';
  lastErrorReason = '';
  lastSuccessMessage = '';
  lastNotificationType: 'success' | 'warning' | '' = '';
  isSubmitting = false;
  showConfirmationModal = false;
  selectedTable: any = null;

  isTableAvailable(table: any): boolean {
    if (!table) {
      return false;
    }

    const raw = table.isAvailable ?? table.IsAvailable ?? table.available ?? table.Available;
    return raw === undefined ? true : !!raw;
  }

  constructor(
    private reservationService: ReservationService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    if (typeof window !== 'undefined' && !this.authService.isLoggedIn()) {
      alert('Rezervasyon yapabilmek için önce giriş yapmalısınız.');
      this.router.navigate(['/login']);
    }
  }

  onSearch() {
    this.lastErrorMessage = '';
    this.lastErrorReason = '';
    this.lastSuccessMessage = '';
    this.lastNotificationType = '';

    this.reservationService
      .getTables(this.filter.date, this.filter.startTime, this.filter.endTime, this.filter.floorId)
      .subscribe({
        next: (data) => {
          this.tables = data;
        },
        error: (err) => {
          console.error('API hatası:', err);
          alert('Masalar getirilirken hata oluştu.');
        }
      });
  }

  onTableSelect(table: any) {
    console.log('Table selected:', table);
    if (this.isSubmitting) {
      console.warn('Already submitting, ignoring click.');
      return;
    }

    if (typeof window !== 'undefined' && !this.authService.isLoggedIn()) {
      alert('Rezervasyon yapmak için lütfen giriş yapınız.');
      this.router.navigate(['/login']);
      return;
    }

    if (!this.isTableAvailable(table)) {
      alert('Bu masa dolu!');
      return;
    }

    this.selectedTable = table;
    // Use setTimeout to ensure change detection picks up the change
    setTimeout(() => {
      this.showConfirmationModal = true;
    });
  }

  cancelReservation() {
    this.showConfirmationModal = false;
    this.selectedTable = null;
  }

  confirmReservation() {
    if (!this.selectedTable) return;

    const studentNumber = this.authService.getCurrentUser();
    if (!studentNumber) {
      alert('Öğrenci numarası bulunamadı. Lütfen tekrar giriş yapın.');
      this.router.navigate(['/login']);
      return;
    }

    this.showConfirmationModal = false;
    this.isSubmitting = true;
    const table = this.selectedTable;
    const selectedTableId = table.id ?? table.Id;
    const reservation = {
      tableId: selectedTableId,
      studentNumber: studentNumber,
      reservationDate: this.filter.date,
      startTime: this.filter.startTime,
      endTime: this.filter.endTime,
      studentType: this.authService.getAcademicLevel() ?? undefined
    };

    this.reservationService.createReservation(reservation)
      .pipe(
        timeout(8000),
        finalize(() => {
          this.isSubmitting = false;
          this.showConfirmationModal = false;
          this.selectedTable = null;
        })
      )
      .subscribe({
        next: (res) => {
          const message = res?.message ?? 'Rezervasyon başarıyla oluşturuldu!';
          this.lastSuccessMessage = message;
          this.lastErrorMessage = '';
          this.lastErrorReason = '';
          this.lastNotificationType = 'success';

          this.tables = this.tables.map(t => {
            if ((t.id ?? t.Id) === selectedTableId) {
              return { ...t, isAvailable: false, IsAvailable: false };
            }
            return t;
          });

          this.reservationService
            .getTables(this.filter.date, this.filter.startTime, this.filter.endTime, this.filter.floorId)
            .subscribe({
              next: refreshed => {
                this.tables = refreshed;
              },
              error: () => {
                // yenileme başarısız olursa mevcut liste kalır
              }
            });
        },
        error: (err) => {
          let message = err?.name === 'TimeoutError'
            ? 'Rezervasyon servisi yanıt vermedi. Lütfen tekrar deneyin.'
            : 'Rezervasyon oluşturulamadı.';
          let reason = '';

          const serverError = err?.error;
          if (typeof serverError === 'string' && serverError.trim().length > 0) {
            message = serverError;
          } else if (serverError && typeof serverError === 'object') {
            if (typeof serverError.message === 'string' && serverError.message.trim().length > 0) {
              message = serverError.message;
            }

            if (typeof serverError.reason === 'string' && serverError.reason.trim().length > 0) {
              reason = serverError.reason;
            }
          }

          this.lastErrorMessage = message;
          this.lastErrorReason = reason;
          this.lastSuccessMessage = '';
          this.lastNotificationType = 'warning';
        }
      });
  }
}
