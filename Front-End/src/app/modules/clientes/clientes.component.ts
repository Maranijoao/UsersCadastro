import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from 'app/core/auth/auth.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
    selector: 'app-clientes',
    standalone: true,
    imports      : [CommonModule, RouterOutlet],
    templateUrl: './clientes.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientesComponent implements OnInit, OnDestroy {
    isAdmin = false;
    private _unsubscribeAll = new Subject<void>();

    constructor(
        private _authService: AuthService,
        private _changeDetectorRef: ChangeDetectorRef,
    ) {
    }

    ngOnInit(): void {
        this._authService.isAdmin$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((isAdmin) => {
                this.isAdmin = isAdmin;
                this._changeDetectorRef.markForCheck();
            });
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }
}