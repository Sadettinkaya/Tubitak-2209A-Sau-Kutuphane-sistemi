import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  standalone: false
})
export class LoginComponent implements OnInit {
  studentNumber: string = '';
  password: string = '';
  isLoading: boolean = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['role'] === 'admin') {
        this.studentNumber = 'admin';
        this.password = 'Admin123!';
        this.cdr.detectChanges();
      }
    });
  }

  onLogin() {
    if (this.isLoading) return;

    if (this.studentNumber && this.password) {
      this.isLoading = true;
      this.cdr.detectChanges();

      this.authService.login(this.studentNumber, this.password).subscribe({
        next: () => {
          this.isLoading = false;
          this.router.navigate(['/']);
        },
        error: (err) => {
          this.isLoading = false;
          this.cdr.detectChanges();
          alert('Giriş başarısız. Lütfen bilgilerinizi kontrol ediniz.');
        }
      });
    } else {
      alert('Lütfen tüm alanları doldurunuz.');
    }
  }
}
