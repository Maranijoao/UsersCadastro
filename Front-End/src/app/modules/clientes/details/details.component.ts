import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UserService } from 'app/core/user/user.service';
import { User, Endereco, ClienteLog } from 'app/core/user/user.types'; // Adicionado ClienteLog
import { Subject, takeUntil, finalize, Observable } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-clientes-details',
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
    MatProgressSpinnerModule
  ],
  templateUrl: './details.component.html'
})
export class ClientesDetailsComponent implements OnInit, OnDestroy {
  rota: string;
  user: User | null = null;
  loading = false;
  isEditMode = false;
  isAdmin = false;
  confirmarsenha: string = '';
  cepLoadingForIndex: number | null = null;
  private _unsubscribeAll = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private service: UserService,
    private cd: ChangeDetectorRef,
    private _snackBar: MatSnackBar,
    private http: HttpClient
  ) { }

  ngOnInit(): void {
    this.service.isAdmin$
      .pipe(takeUntil(this._unsubscribeAll))
      .subscribe(isAdmin => {
        this.isAdmin = isAdmin;
        this.cd.detectChanges();
      });

    this.route.paramMap
      .pipe(takeUntil(this._unsubscribeAll))
      .subscribe((params) => {
        const idParam = params.get('id');

        if (idParam) {
          this.isEditMode = true;
          this.loading = true;
          this.service.getUserById(Number(idParam))
            .pipe(finalize(() => {
              this.loading = false;
              this.cd.detectChanges();
            }))
            .subscribe((user) => {
              if (user) {
                this.user = user;
                if (!this.user.enderecos) this.user.enderecos = [];
                if (!this.user.logs) this.user.logs = [];
              } else {
                this.user = null;
              }
            });
        }
        
        else {
          this.isEditMode = false;
          this.user = {
            id: 0, name: '', email: '', cpf: '', telefone: '', password: '',
            recordStatus: true, role: 'user', enderecos: [], logs: []
          };
        }
      });
  }

  ngOnDestroy(): void {
    this._unsubscribeAll.next();
    this._unsubscribeAll.complete();
  }

  buscarCep(cep: string, endereco: Endereco, index: number): void {
    // Remove caracteres não numéricos para a busca
    const cepLimpo = cep?.replace(/\D/g, '');

    // Valida se o CEP tem o tamanho correto (8 dígitos)
    if (!cepLimpo || cepLimpo.length !== 8) {
      return;
    }

    this.cepLoadingForIndex = index;
    this.cd.detectChanges();

    this.http.get(`https://viacep.com.br/ws/${cepLimpo}/json/`)
      .pipe(finalize(() => {
        this.cepLoadingForIndex = null;
        this.cd.detectChanges();
      }))
      .subscribe({
        next: (dados: any) => {
          if (!dados.erro) {
            endereco.logradouro = dados.logradouro;
            endereco.bairro = dados.bairro;
            endereco.cidade = dados.localidade;
            endereco.uf = dados.uf;
            this.cd.detectChanges();
          } else {

            endereco.logradouro = '';
            endereco.bairro = '';
            endereco.cidade = '';
            endereco.uf = '';
            this._snackBar.open('CEP não encontrado.', 'Fechar', { duration: 3000 });
          }
        },
        error: (err) => {
          console.error('Erro ao buscar CEP:', err);
          this._snackBar.open('Ocorreu um erro ao consultar o CEP.', 'Fechar', { duration: 3000 });
        }
      });
  }

  salvar(): void {
    if (!this.user) return;

    this.loading = true;
    let saveObservable: Observable<any>;

    if (this.isEditMode) {
      saveObservable = this.service.update(this.user);
    }
    else {
      saveObservable = this.service.add(this.user);
    }

    saveObservable
      .pipe(finalize(() => {
        this.loading = false;
        this.cd.detectChanges();
      }))
      .subscribe({
        next: (savedUser) => {
          const message = this.isEditMode ? 'Cliente atualizado com sucesso!' : 'Cliente criado com sucesso!';
          this._snackBar.open(message, 'Fechar', { duration: 3000 });

          if (!this.isEditMode) {
            const newUserId = savedUser?.id || this.user.id;
            this.router.navigate(['../', newUserId], { relativeTo: this.route });
          }
        },
        error: (err) => {
          console.error(err);
          const message = this.isEditMode ? 'Ocorreu um erro ao salvar.' : 'Ocorreu um erro ao criar.';
          this._snackBar.open(message, 'Fechar', { duration: 3000 });
        }
      });
  }

  cancelar(): void {
    this.router.navigate(['/clientes']);
  }

  adicionarEndereco(): void {
    this.user.enderecos.push({
      id: 0,
      clienteId: this.user.id,
      cep: '',
      logradouro: '',
      numero: '',
      complemento: '',
      bairro: '',
      cidade: '',
      uf: ''
    });
    // Diz ao Angular para atualizar a vista após adicionar o endereço
    this.cd.detectChanges();
  }

  removerEndereco(index: number): void {
    if (index > -1 && index < this.user.enderecos.length) {
      this.user.enderecos.splice(index, 1);
    }
  }

  // FUNÇÃO QUE FALTAVA
  trackEnderecoById(index: number, item: Endereco): number {
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
      ? 'bg-emerald-100 text-emerald-700'
      : 'bg-rose-100 text-rose-700';
  }

  roleBadgeClass(role?: string | null): string {
    return role === 'admin'
      ? 'bg-blue-100 text-blue-600'
      : 'bg-slate-100 text-slate-700';
  }
}