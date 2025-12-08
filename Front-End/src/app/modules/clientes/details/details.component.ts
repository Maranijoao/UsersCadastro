import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService } from 'app/core/user/user.service';
import { User, Address } from 'app/core/user/user.types';
import { Subject, takeUntil, finalize, Observable, switchMap, map, of, tap } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatMenuModule } from '@angular/material/menu';
import { AuthService } from 'app/core/auth/auth.service';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NgxMaskDirective } from 'ngx-mask';
import { DateMaskDirective } from 'app/core/shares/directives/date-mask.directive';

@Component({
  selector: 'app-users-details',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    HttpClientModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    DateMaskDirective,
    NgxMaskDirective
  ],

  templateUrl: './details.component.html'
})
export class UserDetailsComponent implements OnInit, OnDestroy {
  user: User | null = null;
  stagedRecordStatus: boolean = true;
  originalUser: User | null = null;
  loading = false;
  isEditMode = false;
  isAdmin = false;
  confirmPassword: string = '';
  cepLoadingForIndex: number | null = null;
  private _unsubscribeAll = new Subject<void>();

  constructor(
    private _activatedRoute: ActivatedRoute,
    private router: Router,
    private _userService: UserService,
    private _authService: AuthService,
    private _changeDetectorRef: ChangeDetectorRef,
    private _snackBar: MatSnackBar,
    private _http: HttpClient
  ) { }

  ngOnInit(): void {
    this._authService.isAdmin$
      .pipe(takeUntil(this._unsubscribeAll))
      .subscribe(isAdmin => {
        this.isAdmin = isAdmin;
        this._changeDetectorRef.markForCheck();
      });

    this._activatedRoute.paramMap
      .pipe(
        map(params => params.get('id')),
        tap(() => { this.loading = true; }),
        switchMap(idParam => {
          if (idParam) {
            this.isEditMode = true;
            return this._userService.getUserById(Number(idParam));
          }
          this.isEditMode = false;
          return of({
            id: 0, name: '', email: '', cpf: '', phoneNumber: '', password: '',
            recordStatus: true, role: 'user', address: [], logs: []
          } as User);
        }),
        takeUntil(this._unsubscribeAll)
      )
      .subscribe(user => {
        if (user) {
          this.user = user;
          this.stagedRecordStatus = user.recordStatus ?? true;
          this.originalUser = this.isEditMode ? JSON.parse(JSON.stringify(user)) : null;
          if (!this.user.address) this.user.address = [];
          if (!this.user.logs) this.user.logs = [];
        } else {
          this.user = null;
          this.originalUser = null;
        }
        this.loading = false;
        this._changeDetectorRef.markForCheck();
      });
  }

  ngOnDestroy(): void {
    this._unsubscribeAll.next();
    this._unsubscribeAll.complete();
    this._userService.notifyListChanged();
  }

  calculateAge(birthDateString?: string): number | null {
    if (!birthDateString) {
      return null;
    }

    const birthDate = new Date(birthDateString.split('/').reverse().join('-'));
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  }

  get displayAge(): string {
    if (!this.user?.birthDate) {
      return '';
    }
    const age = this.calculateAge(this.user.birthDate);
    return age !== null ? `(${age} anos)` : '';

  }

  save(): void {
    if (!this.user) return;
    this.loading = true;

    if (this.user.birthDate != null)
    {
      const date = this.user.birthDate.split('/');
      const day = date[0];
      const month = date[1];
      const year = date[2];
      
      this.user.birthDate = `${year}-${month}-${day}`;
    }

    let saveObservable: Observable<any>;
    const timestamp = new Date().toISOString();
    const changedBy = this._authService.getCurrentUserName();

    // Se for um novo utilizador
    if (!this.isEditMode) {
      this.user.createdBy = changedBy;
      this.user.createdAt = timestamp;
      saveObservable = this._userService.add(this.user);
    }
    // Se for uma edição
    else {
      this.user.updatedBy = changedBy;
      this.user.updatedAt = timestamp;

      if (this.user.recordStatus !== this.originalUser?.recordStatus) {
        if (this.user.recordStatus === false) { // inativado
          this.user.inactivatedAt = timestamp;
          this.user.inactivatedBy = changedBy;
        } else { // reativado
          this.user.inactivatedAt = null;
          this.user.inactivatedBy = null;
        }
      }

      saveObservable = this._userService.update(this.user);
    }

    saveObservable.pipe(
      finalize(() => {
        this.loading = false;
        this._changeDetectorRef.markForCheck();
      })
    ).subscribe({
      next: (savedUser) => {
        this._snackBar.open('Usuário atualizado com sucesso!', 'Fechar', { duration: 3000 });

        this._userService.notifyListChanged();

        if (!this.isEditMode && savedUser?.id) {
          this.router.navigate(['../', savedUser.id], { relativeTo: this._activatedRoute });
        } else if (this.isEditMode) {
          this.ngOnInit();
        }
      },
      error: (err) => {
        console.error(err);
        const message = this.isEditMode ? 'Ocorreu um erro ao salvar as alterações.' : 'Ocorreu um erro ao criar o usuário.';
        this._snackBar.open(message, 'Fechar', { duration: 3000 });
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/clientes']);
  }

  addAddress(): void {
    if (this.user) {
      this.user.address.push({
        id: 0, userId: this.user.id, cep: '', street: '', number: '',
        complement: '', neighborhood: '', city: '', state: ''
      });
      this._changeDetectorRef.markForCheck();
    }
  }

  removeAddrees(index: number): void {
    if (this.user && index > -1 && index < this.user.address.length) {
      this.user.address.splice(index, 1);
    }
  }

  trackAddressById(index: number, item: Address): number {
    return item.id;
  }

  getInitial(n?: string): string {
    const name = (n || '').trim();
    return name.length > 0 ? name[0].toUpperCase() : '?';
  }

  statusText(): string {
    return this.user?.recordStatus ? 'Ativo' : 'Inativo';
  }

  statusClasses(): string {
    return this.user?.recordStatus
      ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-800/20 dark:text-emerald-400'
      : 'bg-rose-100 text-rose-700 dark:bg-rose-700/20 dark:text-rose-400';
  }

  roleBadgeClass(role?: string | null): string {
    if (role === 'admin') {
      return 'bg-blue-100 text-blue-700 dark:bg-blue-500/20 dark:text-blue-400';
    }
    return 'bg-slate-100 text-slate-700 dark:bg-gray-700 dark:text-gray-300';
  }

  searchCep(cep: string, address: Address, index: number): void {
    const cleanCep = cep?.replace(/\D/g, '');
    if (!cleanCep || cleanCep.length !== 8) {
      return;
    }

    this.cepLoadingForIndex = index;
    this._changeDetectorRef.markForCheck();
    this._http.get(`https://viacep.com.br/ws/${cleanCep}/json/`)
      .pipe(finalize(() => {
        this.cepLoadingForIndex = null;
        this._changeDetectorRef.markForCheck();
      }))
      .subscribe({
        next: (data: any) => {
          if (!data.erro) {
            address.street = data.logradouro;
            address.neighborhood = data.bairro;
            address.city = data.localidade;
            address.state = data.uf;
            this._changeDetectorRef.markForCheck();
          } else {
            this._snackBar.open('CEP não encontrado.', 'Fechar', { duration: 3000 });
          }
        },
        error: (err) => {
          console.error('Erro ao buscar CEP:', err);
          this._snackBar.open('Ocorreu um erro ao consultar o CEP.', 'Fechar', { duration: 3000 });
        }
      });
  }
}
