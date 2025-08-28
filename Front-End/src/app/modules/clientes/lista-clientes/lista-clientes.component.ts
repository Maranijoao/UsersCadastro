// import { Component, OnInit, ChangeDetectorRef, NgZone } from '@angular/core';
// import { CommonModule } from '@angular/common';
// import { FormsModule } from '@angular/forms';
// import { User } from 'app/core/user/user.types';
// import { UserService } from 'app/core/user/user.service';
// import { MatIcon } from '@angular/material/icon';
// import { MatLabel, MatFormField } from "@angular/material/form-field";

// @Component({
//   selector: 'app-lista-clientes',
//   standalone: true,
//   templateUrl: './lista-clientes.component.html',
//   imports: [CommonModule, FormsModule, MatIcon, MatLabel, MatFormField]
// })
// export class ListaClientesComponent implements OnInit {
//   Users: User[] = [];
//   UsersInativos: User[] = [];
//   UserEditando: User | null = null;
//   UserParaExcluir: User | null = null;
//   UserParaReativar: User | null = null;

//   exibindoModal = false;
//   exibindoModalExclusao = false;
//   exibindoModalReativacao = false;
//   editando = false;
//   exibindoInativos = true;
//   termoBusca = '';

//   // user: User = {
//   //   id: 0,
//   //   name: '',
//   //   cpf: '',
//   //   email: '',
//   //   telefone: '',
//   //   password: ''
//   // };

//   // UserModal: User = {
//   //   id: 0,
//   //   name: '',
//   //   cpf: '',
//   //   email: '',
//   //   telefone: '',
//   //   password: ''
//   // };

//   mensagemModalSucesso: string | null = null;
//   mensagemModalErro: string | null = null;

//   constructor(
//     private UserService: UserService,
//     private cdr: ChangeDetectorRef,
//     private zone: NgZone
//   ) { }

//   ngOnInit(): void {
//     this.carregarUsers();
//     this.carregarUsersInativos();
//   }

//   trackById(index: number, User: User): number {
//     return User.id;
//   }

//   carregarUsers(): void {
//     this.UserService.getAll(this.termoBusca).subscribe({
//       next: (data) => (this.Users = data),
//       error: (err) => console.error('Erro ao buscar Clientes', err)
//     });
//   }

//   carregarUsersInativos(): void {
//     this.UserService.getAllInativos(this.termoBusca).subscribe({
//       next: (data) => (this.UsersInativos = data),
//       error: (err) => console.error('Erro ao buscar Clientes', err)
//     });
//   }

//   buscar(): void {
//     this.carregarUsers();
//     this.carregarUsersInativos();
//   }

//   alternarUsers(): void {
//     this.exibindoInativos = !this.exibindoInativos;
//     this.buscar();
//   }

//   // abrirAdicao(): void {
//   //   this.editando = false;
//   //   this.exibindoModal = true;
//   //   this.UserModal = { id: 0, name: '', cpf: '', email: '', telefone: '', password: '' };
//   //   this.limparMensagens();
//   // }

//   abrirEdicao(User: User): void {
//     this.editando = true;
//     this.exibindoModal = true;
//     this.UserModal = { ...User };
//     this.limparMensagens();
//   }

//   abrirExclusao(User: User): void {
//     this.exibindoModalExclusao = true;
//     this.UserParaExcluir = User;
//     this.limparMensagens();
//   }

//   abrirReativacao(User: User): void {
//     this.exibindoModalReativacao = true;
//     this.UserParaReativar = User;
//     this.limparMensagens();
//   }

//   fecharModal(): void {
//     this.exibindoModal = false;
//   }

//   salvarUser(): void {
//     const callback = () => {
//       this.fecharModal();
//       this.buscar();
//     };

//     if (this.editando) {
//       this.UserService.update(this.UserModal).subscribe({
//         next: () => this.sucesso('Cliente atualizado com sucesso!', callback),
//         error: (err) => this.erro('Erro ao atualizar Cliente: ' + err.message)
//       });
//     } else {
//       this.UserService.add(this.UserModal).subscribe({
//         next: () => this.sucesso('Cliente adicionado com sucesso!', callback),
//         error: (err) => this.erro('Erro ao adicionar Cliente: ' + err.message)
//       });
//     }
//   }

//   excluirUser(): void {
//     if (!this.UserParaExcluir) return;

//     this.UserService.delete(this.UserParaExcluir.id).subscribe({
//       next: () => {
//         this.exibindoModalExclusao = false;
//         this.buscar();
//       },
//       error: (err) => {
//         // Trate o erro aqui, opcional
//         console.error('Erro ao excluir cliente:', err);
//       }
//     });
//   }

//   reativarUser(): void {
//     if (!this.UserParaReativar) return;

//     this.UserService.reativar(this.UserParaReativar).subscribe({
//       next: () => {
//         this.exibindoModalReativacao = false;
//         this.buscar();
//       },
//       error: (err) => {
//         // Trate o erro aqui, opcional
//         console.error('Erro ao reativar cliente:', err);
//       }
//     });
//   }

//   sucesso(msg: string, cb?: () => void): void {
//     this.mensagemModalSucesso = msg;
//     this.mensagemModalErro = null;
//     setTimeout(() => {
//       this.mensagemModalSucesso = null;
//       cb?.();
//     }, 3000);
//   }

//   erro(msg: string): void {
//     this.mensagemModalErro = msg;
//     this.mensagemModalSucesso = null;
//     setTimeout(() => (this.mensagemModalErro = null), 3000);
//   }

//   limparMensagens(): void {
//     this.mensagemModalSucesso = null;
//     this.mensagemModalErro = null;
//   }
// }
