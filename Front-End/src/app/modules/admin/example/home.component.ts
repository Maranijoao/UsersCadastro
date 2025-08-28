import { Component, OnInit, ViewEncapsulation, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner'; // Importado para o spinner
import { UserService } from 'app/core/user/user.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
    selector     : 'home',
    templateUrl  : './home.component.html',
    encapsulation: ViewEncapsulation.None,
    standalone   : true,
    imports      : [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule], 
})
export class HomeComponent implements OnInit, OnDestroy
{
    totalClientes = 0;
    loadingClientes = true;
    private _unsubscribeAll = new Subject<void>();

    constructor(
        private _userService: UserService,
        private _changeDetectorRef: ChangeDetectorRef,
    )
    {
    }

    ngOnInit(): void {
        this._userService.allUsers$
        .pipe(takeUntil(this._unsubscribeAll))
        .subscribe((users) =>{
            this.totalClientes = users ? users.length : 0;
            this.loadingClientes = false;
            this._changeDetectorRef.markForCheck();
        });

        this._userService.getAllUsersCombined().subscribe();
    }

    ngOnDestroy(): void{
      this._unsubscribeAll.next();
      this._unsubscribeAll.complete();
    }
}
