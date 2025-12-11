import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FeedbackService } from '../services/feedback.service';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-feedback',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mt-5">
      <div class="card">
        <div class="card-header bg-info text-white">
          <h3>Geri Bildirim</h3>
        </div>
        <div class="card-body">
          <form (ngSubmit)="onSubmit()">
            <div class="mb-3">
              <label for="message" class="form-label">Mesajınız</label>
              <textarea class="form-control" id="message" rows="4" [(ngModel)]="message" name="message" required></textarea>
            </div>
            <button type="submit" class="btn btn-primary">Gönder</button>
          </form>
        </div>
      </div>
    </div>
  `
})
export class FeedbackComponent {
  message: string = '';

  constructor(
    private feedbackService: FeedbackService,
    private authService: AuthService,
    private router: Router
  ) {}

  onSubmit() {
    const studentNumber = this.authService.getCurrentUser();
    if (!studentNumber) {
      this.router.navigate(['/login']);
      return;
    }
    if (this.authService.isAdmin()) {
      alert('Yönetici olarak geri bildirim gönderemezsiniz.');
      return;
    }

    const feedback = {
      studentNumber: studentNumber,
      message: this.message
    };

    this.feedbackService.submitFeedback(feedback).subscribe({
      next: () => {
        alert('Geri bildiriminiz için teşekkürler!');
        this.message = '';
      },
      error: (err) => {
        console.error(err);
        alert('Bir hata oluştu.');
      }
    });
  }
}
