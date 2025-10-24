import { Component, OnInit, OnDestroy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { UserService, ChartDataPoint } from 'app/core/user/user.service';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FuseConfig, FuseConfigService } from '@fuse/services/config';

import {
    ApexAxisChartSeries,
    ApexChart,
    ApexXAxis,
    ApexYAxis,
    ApexTitleSubtitle,
    ApexStroke,
    ApexGrid,
    ApexDataLabels,
    ApexTooltip,
    NgApexchartsModule,
    ChartComponent
} from 'ng-apexcharts';

export type ChartOptions = {
    series: ApexAxisChartSeries;
    chart: ApexChart;
    xaxis: ApexXAxis;
    yaxis: ApexYAxis;
    title: ApexTitleSubtitle;
    stroke: ApexStroke;
    grid: ApexGrid;
    dataLabels: ApexDataLabels;
    tooltip: ApexTooltip;
};

@Component({
    selector: 'app-users-chart',
    standalone: true,
    imports: [CommonModule, NgApexchartsModule, MatProgressSpinnerModule],
    templateUrl: './users-chart.component.html',
})
export class UsersChartComponent implements OnInit, OnDestroy {
    @ViewChild('chart') chart: ChartComponent;
    public chartOptions: Partial<ChartOptions>;
    config: FuseConfig;

    private _unsubscribeAll = new Subject<void>();
    private chartData: ChartDataPoint[] = [];
    loading = true;

    constructor(
        private _userService: UserService,
        private _fuseConfigService: FuseConfigService, 
        private _cdr: ChangeDetectorRef
    ) { }

    ngOnInit(): void {
        this.fetchChartData();

        this._userService.listNeedsRefresh$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(() => {
                this.fetchChartData();
            });

        this._fuseConfigService.config$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((config: FuseConfig) => {
                this.config = config;
                this.configureChartOptions();
            });
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }

    fetchChartData(): void {
        this.loading = true;
        this._userService.getUserRegistrationsByDay().subscribe(data => {
            this.chartData = data;
            this.configureChartOptions(); 
            this.loading = false;
            this._cdr.markForCheck();
        });
    }

    configureChartOptions(): void {
        if (!this.config) {
            return;
        }
 
        const isDark = this.config.scheme === 'dark';

        this.chartOptions = {
            series: [{
                name: 'Novos Usuários',
                data: this.chartData
            }],
            chart: {
                height: 350,
                type: 'area',
                background: 'transparent',
                toolbar: { show: true, tools: { download: false } }
            },
            dataLabels: { enabled: false },
            stroke: { curve: 'smooth', width: 2 },
            grid: {
                borderColor: isDark ? '#374151' : '#e7e7e7',
                strokeDashArray: 5,
            },
            title: {
                text: 'Registros de Usuários por Dia',
                align: 'left',
                style: {
                    fontSize: '16px',
                    fontWeight: '600',
                    color: isDark ? '#E2E8F0' : '#374151'
                }
            },
            xaxis: {
                type: 'datetime',
                labels: {
                    datetimeUTC: false,
                    style: { colors: isDark ? '#9CA3AF' : '#6B7280' }
                }
            },
            yaxis: {
                labels: {
                    style: { colors: isDark ? '#9CA3AF' : '#6B7280' }
                }
            },
            tooltip: {
                theme: isDark ? 'dark' : 'light'
            }
        };
        this._cdr.markForCheck();
    }
}
