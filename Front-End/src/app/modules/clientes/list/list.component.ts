import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute, RouterOutlet } from '@angular/router';
import { debounceTime, finalize, Subject, takeUntil } from 'rxjs';
import { PagedResult, UserService } from 'app/core/user/user.service';
import { User } from 'app/core/user/user.types';
import { FormsModule } from '@angular/forms';
import { AuthService } from 'app/core/auth/auth.service';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { SidebarComponent, StatusFilter, RoleFilter } from '../sidebar/sidebar.component';

@Component({
    selector: 'app-clientes-list',
    standalone: true,
    imports: [CommonModule, RouterOutlet, FormsModule, MatButtonModule, MatIconModule, MatPaginatorModule, SidebarComponent],
    templateUrl: './list.component.html'
})

export class ClientesListComponent implements OnInit, OnDestroy {
    users: User[] = [];
    pagination: PagedResult<User> | null = null;
    filtered: User[] = [];
    loading = false;
    selectedId?: number;
    search: string = '';
    isAdmin = false;
    pageNumber: number = 1;
    pageSize: number = 10;
    roleFilter: RoleFilter = 'all';
    statusFilter: StatusFilter = 'all';

    pageSizeOptions: number[] = [5, 10, 25, 50];

    private searchSubject = new Subject<string>();
    private _unsubscribeAll = new Subject<void>();

    constructor(
        private service: UserService,
        private _authService: AuthService,
        private router: Router,
        private route: ActivatedRoute,
        private cd: ChangeDetectorRef
    ) { }

    ngOnInit(): void {
        this.service.pagination$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((paginationData) => {
                if (paginationData) {
                    this.pagination = paginationData;
                    this.users = paginationData.items;
                    this.filtered = paginationData.items;
                    this.cd.markForCheck();
                }
            });

        this._authService.isAdmin$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(isAdmin => {
                this.isAdmin = isAdmin;
                this.cd.markForCheck();
            });

        this.searchSubject.pipe(
            debounceTime(500),
            takeUntil(this._unsubscribeAll)
        ).subscribe(() => {
            this.pageNumber = 1;
            this.fetchUsers();
        });

        this.service.listNeedsRefresh$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe(() => {
                this.fetchUsers();
            })

        this.fetchUsers();

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

    fetchUsers(): void {
        this.loading = true;
        this.service.getUsers(this.search, this.pageNumber, this.pageSize, this.roleFilter, this.statusFilter)
            .pipe(finalize(() => {
                this.loading = false;
                this.cd.markForCheck();
            }))
            .subscribe();
    }

    onPageChange(event: PageEvent): void {
        this.pageNumber = event.pageIndex + 1;
        this.pageSize = event.pageSize;
        this.fetchUsers();
    }

    onStatusFilterChanged(status: StatusFilter): void {
        this.statusFilter = status;
        this.pageNumber = 1;
        this.fetchUsers();
    }

    onRoleFilterChanged(role: RoleFilter): void {
        this.roleFilter = role;
        this.pageNumber = 1;
        this.fetchUsers();
    }

    onSearchChanged(): void {
        this.searchSubject.next(this.search);
    }

    goToNextPage(): void {
        if (this.pagination.hasNextPage) {
            this.pageNumber++;
            this.fetchUsers();
        }
    }

    goToPreviousPage(): void {
        if (this.pageNumber > 1) {
            this.pageNumber--;
            this.fetchUsers();
        }
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
        if (role === 'admin') {
            return 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-400';
        }
        return 'bg-slate-100 text-slate-700 dark:bg-gray-700 dark:text-gray-300';
    }

    trackById = (_: number, c: User) => c.id;
}
