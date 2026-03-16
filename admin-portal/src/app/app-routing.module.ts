// angular import
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

// Project import
import { AdminLayout } from './theme/layouts/admin-layout/admin-layout.component';
import { GuestLayoutComponent } from './theme/layouts/guest-layout/guest-layout.component';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

const routes: Routes = [
  {
    path: 's/:id',
    loadComponent: () =>
      import('./pages/public-survey/public-survey.component').then((c) => c.PublicSurveyComponent)
  },
  {
    path: '',
    component: AdminLayout,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: '/surveys',
        pathMatch: 'full'
      },
      {
        path: 'dashboard/default',
        loadComponent: () => import('./demo/dashboard/default/default.component').then((c) => c.DefaultComponent)
      },
      {
        path: 'typography',
        loadComponent: () => import('./demo/component/basic-component/typography/typography.component').then((c) => c.TypographyComponent)
      },
      {
        path: 'color',
        loadComponent: () => import('./demo/component/basic-component/color/color.component').then((c) => c.ColorComponent)
      },
      {
        path: 'sample-page',
        loadComponent: () => import('./demo/others/sample-page/sample-page.component').then((c) => c.SamplePageComponent)
      },
      {
        path: 'surveys',
        loadComponent: () => import('./pages/surveys/surveys-list/surveys-list.component').then((c) => c.SurveysListComponent)
      },
      {
        path: 'surveys/:id',
        loadComponent: () => import('./pages/surveys/survey-detail/survey-detail.component').then((c) => c.SurveyDetailComponent)
      },
      {
        path: 'users',
        loadComponent: () => import('./pages/users/users-list/users-list.component').then((c) => c.UsersListComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./pages/profile/profile.component').then((c) => c.ProfileComponent)
      }
    ]
  },
  {
    path: '',
    component: GuestLayoutComponent,
    canActivate: [guestGuard],
    children: [
      {
        path: 'login',
        loadComponent: () => import('./demo/pages/authentication/auth-login/auth-login.component').then((c) => c.AuthLoginComponent)
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./demo/pages/authentication/auth-register/auth-register.component').then((c) => c.AuthRegisterComponent)
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
