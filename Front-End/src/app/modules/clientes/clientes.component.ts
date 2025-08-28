import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { sidebarComponent } from './sidebar/sidebar.component'; // Assumindo que este é o seu componente da sidebar
import { UserService } from 'app/core/user/user.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
    selector     : 'app-clientes',
    standalone   : true,
    imports      : [CommonModule, RouterOutlet, sidebarComponent],
    templateUrl  : './clientes.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ClientesComponent implements OnInit, OnDestroy
{
    isAdmin = false;
    private _unsubscribeAll = new Subject<void>();

    /**
     * Constructor
     */
    constructor(
        private _userService: UserService,
        private _changeDetectorRef: ChangeDetectorRef,
    )
    {
    }

    ngOnInit(): void
    {
        // Inscreve-se para saber se o utilizador logado é um administrador
        this._userService.isAdmin$
            .pipe(takeUntil(this._unsubscribeAll))
            .subscribe((isAdmin) =>
            {
                this.isAdmin = isAdmin;
                this._changeDetectorRef.markForCheck();
            });
    }

    ngOnDestroy(): void
    {
        this._unsubscribeAll.next();
        this._unsubscribeAll.complete();
    }
}
