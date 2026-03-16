// angular import
import { Component, output, inject, input } from '@angular/core';
import { RouterModule } from '@angular/router';

// project import
import { SharedModule } from 'src/app/theme/shared/shared.module';
import { AuthService } from 'src/app/core/auth.service';

// icon
import { IconService } from '@ant-design/icons-angular';
import { LogoutOutline, EditOutline } from '@ant-design/icons-angular/icons';

@Component({
  selector: 'app-nav-right',
  imports: [SharedModule, RouterModule],
  templateUrl: './nav-right.component.html',
  styleUrls: ['./nav-right.component.scss']
})
export class NavRightComponent {
  private iconService = inject(IconService);
  private auth = inject(AuthService);

  // public props
  styleSelectorToggle = input<boolean>();
  currentUser = this.auth.getCurrentUser();
  readonly Customize = output();
  windowWidth: number;
  screenFull: boolean = true;
  direction: string = 'ltr';

  constructor() {
    this.windowWidth = window.innerWidth;
    this.iconService.addIcon(LogoutOutline, EditOutline);
  }

  profile = [
    { icon: 'edit', title: 'Edit Profile' },
    { icon: 'logout', title: 'Logout' }
  ];

  logout(): void {
    this.auth.logout();
  }
}
