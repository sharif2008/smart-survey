import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SharedModule } from 'src/app/theme/shared/shared.module';
import { AuthService } from 'src/app/core/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [SharedModule, RouterLink],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);

  name = '';
  email = '';
  role: string | undefined;
  saved = false;

  ngOnInit(): void {
    const user = this.auth.getCurrentUser();
    if (user) {
      this.name = user.name;
      this.email = user.email;
      this.role = user.role;
    }
  }

  save(): void {
    const trimmed = (this.name || '').trim();
    if (!trimmed) return;

    this.auth.updateProfile(trimmed).subscribe((result) => {
      if (result.success) {
        this.name = trimmed;
        this.saved = true;
        setTimeout(() => {
          this.saved = false;
        }, 2000);
      } else if (!result.success && 'error' in result) {
        alert(result.error || 'Failed to save profile.');
      } else {
        alert('Failed to save profile.');
      }
    });
  }
}

