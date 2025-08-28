import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink, ActivatedRoute, RouterOutlet } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { UserService } from 'app/core/user/user.service';
import { User } from 'app/core/user/user.types';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-clientes-list',
  standalone: true,
  imports: [CommonModule, RouterOutlet, FormsModule],
  templateUrl: './list.component.html'
})
export class ClientesListComponent implements OnInit, OnDestroy {
  clientes: User[] = [];
  filtered: User[] = [];
  loading = false;
  selectedId?: number;
  search = '';
  isAdmin = false;
  private _unsubscribeAll = new Subject<void>();

  constructor(
    private service: UserService,
    private router: Router,
    private route: ActivatedRoute,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loading = true;

     this.service.isAdmin$
        .pipe(takeUntil(this._unsubscribeAll))
        .subscribe(isAdmin => {
            this.isAdmin = isAdmin;
            this.cd.detectChanges();
        });

    this.service.allUsers$
      .pipe(takeUntil(this._unsubscribeAll))
      .subscribe((listaCompleta) => {
        this.clientes = listaCompleta;
        this.applyFilter();
        this.loading = false;
        this.cd.detectChanges(); 
      });

    this.service.getAllUsersCombined().subscribe();
    
    this.route.firstChild?.paramMap
      .pipe(takeUntil(this._unsubscribeAll))
      .subscribe((p) => {
        const id = Number(p?.get('id'));
        this.selectedId = isNaN(id) ? undefined : id;
      });
}

  ngOnDestroy(): void {
    this._unsubscribeAll.next();
    this._unsubscribeAll.complete();
  }

  applyFilter(): void {
    const q = (this.search || '').trim().toLowerCase();
    if (!q) {
      this.filtered = this.clientes;
      return;
    }
    this.filtered = this.clientes.filter((c) => {
      return (
        (c.name || '').toLowerCase().includes(q) ||
        (c.email || '').toLowerCase().includes(q) ||
        (c.cpf || '').toLowerCase().includes(q) ||
        (c.telefone || '').toLowerCase().includes(q)
      );
    });
  }

  select(c: User): void {
    this.selectedId = c.id;
    this.router.navigate([c.id], { relativeTo: this.route });
  }

  getInitial(n?: string): string {
    const name = (n || '').trim();
    if (!name) return '?';
    return name[0].toUpperCase();
  }

  statusDotClass(active: boolean | null | undefined): string {
    return active ? 'bg-emerald-500' : 'bg-rose-500';
  }

  roleBadgeClass(role?: string | null): string {
    if (role === 'admin')
      return 'bg-blue-100 text-blue-600 dark:bg-blue-500/10 dark:text-blue-300';
    return 'bg-slate-100 text-slate-700 dark:bg-slate-500/10 dark:text-slate-300';
  }

  trackById = (_: number, c: User) => c.id;
}
