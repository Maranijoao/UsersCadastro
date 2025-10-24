import { NgClass, NgIf } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit, ViewEncapsulation } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { Router } from '@angular/router';
import { User } from 'app/core/user/user.types';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from 'app/core/auth/auth.service';
import { FuseConfig, FuseConfigService, Scheme } from '@fuse/services/config'; // Importar dependências do Fuse

@Component({
    selector: 'user',
    templateUrl: './user.component.html',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    exportAs: 'user',
    standalone: true,
    imports: [MatButtonModule, MatMenuModule, NgIf, MatIconModule, NgClass, MatDividerModule],
})
export class UserComponent implements OnInit, OnDestroy {
    @Input() showAvatar: boolean = true;
    user: User | null = null;
    config: FuseConfig;
    scheme: Scheme;

    private _unsubscribeAll: Subject<any> = new Subject<any>();

    constructor(
        private _changeDetectorRef: ChangeDetectorRef,
        private _router: Router,
        private _authService: AuthService,
        private _fuseConfigService: FuseConfigService, // Injetar o serviço de configuração do Fuse
    ) {
    }

    ngOnInit(): void {
        // Inscrever-se para obter os dados do usuário
        this._authService.user$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((user: User | null) => {
                this.user = user;
                this._changeDetectorRef.markForCheck();
            });

        // Inscrever-se para obter as configurações de tema do Fuse
        this._fuseConfigService.config$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((config: FuseConfig) => {
                this.config = config;
                this.scheme = config.scheme;
                this._changeDetectorRef.markForCheck();
            });
    }

    ngOnDestroy(): void {
        this._unsubscribeAll.next(null);
        this._unsubscribeAll.complete();
    }

    /**
     * Define o esquema de cores (dark/light) no serviço de configuração do Fuse.
     */
    setScheme(scheme: Scheme): void {
        this._fuseConfigService.config = { scheme };
    }

    updateUserStatus(status: string): void {
        if (!this.user) {
            return;
        }
        this.user.status = status;
        this._changeDetectorRef.markForCheck();
    }

    getStatusColorClass(): string {
        if (!this.user || !this.user.status) {
            return 'bg-green-500';
        }
        switch (this.user.status) {
            case 'online': return 'bg-green-500';
            case 'away': return 'bg-amber-500';
            case 'busy': return 'bg-red-500';
            case 'not-visible':
            default: return 'bg-gray-400';
        }
    }

    signOut(): void {
        this._router.navigate(['/sign-out']);
    }
}