import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth.service';

@Component({
  selector: 'app-auth-login',
  imports: [RouterModule, ReactiveFormsModule],
  templateUrl: './auth-login.component.html',
  styleUrl: './auth-login.component.scss'
})
export class AuthLoginComponent implements OnInit {
  loginForm: FormGroup;
  loginError = '';

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.nonNullable.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      keepSignedIn: [true]
    });
  }

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.router.navigate(['/dashboard/default']);
    }
  }

  loading = false;

  onSubmit(): void {
    this.loginError = '';
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    this.loading = true;
    const { email, password } = this.loginForm.getRawValue();
    this.auth.login(email, password).subscribe({
      next: (result) => {
        this.loading = false;
        if (result.success) {
          this.router.navigate(['/dashboard/default']);
        } else {
          this.loginError = 'error' in result ? result.error : 'Login failed.';
        }
      },
      error: () => {
        this.loading = false;
        this.loginError = 'Unable to reach the server. Is the API running at http://localhost:5008?';
      }
    });
  }
}
