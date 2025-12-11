import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.css']
})
export class SignupComponent {
  user = {
    studentNumber: '',
    fullName: '',
    email: '',
    password: '',
    academicLevel: 'Lisans'
  };

  constructor(private authService: AuthService, private router: Router) {}

  onSignup() {
    this.authService.register(this.user).subscribe({
      next: () => {
        alert('Kayıt başarılı! Giriş yapabilirsiniz.');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        console.error(err);
        let errorMessage = 'Kayıt başarısız.\n';
        if (err.error && Array.isArray(err.error)) {
            err.error.forEach((e: any) => errorMessage += `- ${e.description}\n`);
        } else if (err.error && err.error.message) {
            errorMessage += err.error.message;
        } else {
            errorMessage += 'Lütfen bilgilerinizi kontrol ediniz.';
        }
        alert(errorMessage);
      }
    });
  }
}
