import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { Subject, finalize, takeUntil, debounceTime } from 'rxjs';
import { SimulationService } from 'app/core/simulation/simulation.service'; // Reutilizamos o service por enquanto
import { PagedSimulationResult, SimulationListItem } from 'app/core/simulation/simulation.types';

@Component({
  selector: 'app-loan-list',
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
  templateUrl: './loan-list.component.html',
})
export class LoanListComponent implements OnInit, OnDestroy {

  pagination: PagedSimulationResult | null = null;
  loans: SimulationListItem[] = [];
  
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
      this.fetchLoans();
    });

    this.fetchLoans();
  }

  ngOnDestroy(): void {
    this._unsubscribeAll.next();
    this._unsubscribeAll.complete();
  }

  fetchLoans(): void {
    this.loading = true;
    // Aqui no futuro você pode filtrar apenas status "Contratado" se tiver
    this._simulationService.getSimulations(this.search, this.pageNumber, this.pageSize)
      .pipe(
        finalize(() => {
          this.loading = false;
          this._cd.markForCheck();
        })
      )
      .subscribe((pagedResult) => {
        this.pagination = pagedResult;
        this.loans = pagedResult.items;
      });
  }

  onSearchChanged(): void {
    this.searchSubject.next(this.search);
  }

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.fetchLoans();
  }

  goToDetails(id: number): void {
    this._router.navigate([id], { relativeTo: this._route });
  }

  trackById(index: number, item: SimulationListItem): number {
    return item.id;
  }
}