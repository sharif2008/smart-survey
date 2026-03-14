import { Component, OnInit } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/auth.service';

@Component({
  selector: 'app-auth-register',
  imports: [RouterModule, ReactiveFormsModule],
  templateUrl: './auth-register.component.html',
  styleUrl: './auth-register.component.scss'
})
export class AuthRegisterComponent implements OnInit {
  registerForm: FormGroup;
  registerError = '';
  loading = false;
  roles = [
    { value: 'Researcher', label: 'Researcher' },
    { value: 'Admin', label: 'Admin' }
  ] as const;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.nonNullable.group({
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      role: ['Researcher' as const, Validators.required]
    });
  }

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.router.navigate(['/dashboard/default']);
    }
  }

  onSubmit(): void {
    this.registerError = '';
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }
    this.loading = true;
    const dto = this.registerForm.getRawValue();
    this.auth.register(dto).subscribe({
      next: (result) => {
        this.loading = false;
        if (result.success) {
          this.router.navigate(['/dashboard/default']);
        } else {
          this.registerError = result.success === false ? result.error : 'Registration failed.';
        }
      },
      error: () => {
        this.loading = false;
        this.registerError = 'Unable to reach the server. Is the API running?';
      }
    });
  }
}
