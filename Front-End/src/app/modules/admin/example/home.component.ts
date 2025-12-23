import { Component, OnInit, ViewEncapsulation, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserService } from 'app/core/user/user.service';
import { SimulationService } from 'app/core/simulation/simulation.service';
import { DashboardTotals } from 'app/core/simulation/simulation.types';
import { Subject, takeUntil } from 'rxjs';
import { UsersChartComponent } from 'app/modules/dashboard/users-chart/users-chart.component';
import { SimulationsBarChartComponent } from 'app/modules/dashboard/simulations-chart/simulations-bar-chart.component';

@Component({
    selector: 'home',
    templateUrl: './home.component.html',
    encapsulation: ViewEncapsulation.None,
    standalone: true,
    imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, UsersChartComponent, SimulationsBarChartComponent],
})
export class HomeComponent implements OnInit, OnDestroy {
    totalClientes = 0;
    loadingClientes = true;
    totalSimulations = 0;
    loadingSimulations = true;
    totalContractValue = 0;
    totalReleasedAmount = 0;
    loadingValues = true;

    private _unsubscribeAll = new Subject<void>();

    constructor(
        private _userService: UserService,
        private _simulationService: SimulationService,
        private _changeDetectorRef: ChangeDetectorRef,
    ) {
    }

    ngOnInit(): void {
        this._userService.getUsers().subscribe();

        this._userService.pagination$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((pagination) => {
                this.totalClientes = pagination?.totalCount;
                this.loadingClientes = false;
                this._changeDetectorRef.markForCheck();
            });

        this._simulationService.getSimulations().subscribe();

        this._simulationService.pagination$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((pagination) => {
                this.totalSimulations = pagination?.totalCount;
                this.loadingSimulations = false;
                this._changeDetectorRef.markForCheck();
            });

        this.loadingValues = true;
        
        this._simulationService.getSimulationTotals()
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((totals: DashboardTotals) => {
                this.totalReleasedAmount = totals.totalReleasedAmount;
                this.totalContractValue = totals.totalContractValue;
                this.loadingValues = false;
                this._changeDetectorRef.markForCheck();
            });
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }
}