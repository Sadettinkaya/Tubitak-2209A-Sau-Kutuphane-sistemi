import { Component, OnInit } from '@angular/core';
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

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['role'] === 'admin') {
        this.studentNumber = 'admin';
        this.password = 'Admin123!';
      }
    });
  }

  onLogin() {
    if (this.studentNumber && this.password) {
      this.authService.login(this.studentNumber, this.password).subscribe({
        next: () => {
          this.router.navigate(['/']);
        },
        error: (err) => {
          alert('Giriş başarısız. Lütfen bilgilerinizi kontrol ediniz.');
        }
      });
    } else {
      alert('Lütfen tüm alanları doldurunuz.');
    }
  }
}
