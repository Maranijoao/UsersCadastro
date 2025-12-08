import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner'; // Importe o Spinner
import { FormsModule } from '@angular/forms';
import { Subject, Observable, debounceTime, finalize, takeUntil } from 'rxjs';
import { SimulationService } from 'app/core/simulation/simulation.service';
import { PagedSimulationResult, SimulationListItem } from 'app/core/simulation/simulation.types';

@Component({
  selector: 'app-list', 
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule, 
  ],
  templateUrl: './list.component.html',
})
export class SimulationListComponent implements OnInit, OnDestroy {

  pagination: PagedSimulationResult | null = null;
  simulations: SimulationListItem[] = [];
  
  loading = false;
  search: string = '';
  pageNumber: number = 1;
  pageSize: number = 10;
  
  private searchSubject = new Subject<string>();
  private _unsubscribeAll = new Subject<void>();

  constructor(
    private _simulationService: SimulationService,
    private _router: Router,
    private _route: ActivatedRoute, 
    private _cd: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
   
    this.searchSubject.pipe(
      debounceTime(500), 
      takeUntil(this._unsubscribeAll)
    ).subscribe(() => {
      this.pageNumber = 1; 
      this.fetchSimulations();
    });

    this._simulationService.listNeedsRefresh$
      .pipe(takeUntil(this._unsubscribeAll))
      .subscribe(() => {
        this.fetchSimulations();
      });

    this.fetchSimulations();
  }

  ngOnDestroy(): void {
    this._unsubscribeAll.next();
    this._unsubscribeAll.complete();
  }

  fetchSimulations(): void {
    this.loading = true;
    this._simulationService.getSimulations(this.search, this.pageNumber, this.pageSize)
      .pipe(
        finalize(() => {
          this.loading = false;
          this._cd.markForCheck();
        })
      )
      .subscribe((pagedResult) => {
        this.pagination = pagedResult;
        this.simulations = pagedResult.items;
      });
  }

  onSearchChanged(): void {
    this.searchSubject.next(this.search);
  }

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.fetchSimulations();
  }

  goToDetails(id: number | 'new'): void {
    this._router.navigate([id], { relativeTo: this._route });
  }

  trackById(index: number, item: SimulationListItem): number {
    return item.id;
  }
}