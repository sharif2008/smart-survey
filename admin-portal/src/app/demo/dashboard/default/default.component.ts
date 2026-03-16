// angular import
import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

// project import
import { CardComponent } from 'src/app/theme/shared/components/card/card.component';
import { AuthService } from 'src/app/core/auth.service';
import { DashboardApiService } from 'src/app/core/services/dashboard-api.service';
import type { DashboardStatsDto } from 'src/app/core/api/dashboard-api.model';
import { NgApexchartsModule, type ApexOptions } from 'ng-apexcharts';

@Component({
  selector: 'app-default',
  imports: [CommonModule, RouterLink, CardComponent, NgApexchartsModule],
  templateUrl: './default.component.html',
  styleUrls: ['./default.component.scss']
})
export class DefaultComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly dashboardApi = inject(DashboardApiService);

  stats: DashboardStatsDto | null = null;
  loading = true;
  error: string | null = null;
  hourlyChart: Partial<ApexOptions> | null = null;

  get isAdmin(): boolean {
    return this.auth.getCurrentUser()?.role === 'Admin';
  }

  ngOnInit(): void {
    this.dashboardApi.getStats().subscribe({
      next: (s) => {
        this.stats = s;
        this.hourlyChart = this.buildHourlyChart(s);
        this.loading = false;
        this.error = null;
      },
      error: () => {
        this.stats = { totalSurveys: 0, researcherCount: 0, surveySummaries: [], hourlyResponses: [] };
        this.hourlyChart = null;
        this.loading = false;
        this.error = 'Could not load dashboard stats.';
      }
    });
  }

  private buildHourlyChart(stats: DashboardStatsDto): Partial<ApexOptions> | null {
    const values = stats.hourlyResponses ?? [];
    if (!values.length) return null;
    const labels = values.map((_, idx) => `-${values.length - 1 - idx}h`);
    return {
      chart: { type: 'bar', height: 260, toolbar: { show: false }, background: 'transparent' },
      series: [{ name: 'Responses', data: values }],
      xaxis: { categories: labels },
      dataLabels: { enabled: false },
      plotOptions: { bar: { borderRadius: 4, columnWidth: '55%' } }
    };
  }
}
