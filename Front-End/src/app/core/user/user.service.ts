import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, ReplaySubject, forkJoin, map, switchMap } from 'rxjs'; // 1. Adicionado 'map'
import { tap } from 'rxjs/operators';
import { User } from './user.types';

@Injectable({ providedIn: 'root' })
export class UserService {
    private readonly _httpClient = inject(HttpClient);
    private readonly _baseUrl = 'http://localhost:5263/api/Clientes';

    private readonly _user = new ReplaySubject<User>(1);
    private readonly _Users: ReplaySubject<User[]> = new ReplaySubject<User[]>(1);
    private readonly _UsersInativos: ReplaySubject<User[]> = new ReplaySubject<User[]>(1);
    private readonly _allUsers = new ReplaySubject<User[]>(1);
    // -----------------------------------------------------------------------------------------------------
    // @ Accessors
    // -----------------------------------------------------------------------------------------------------

    set user(value: User) {
        this._user.next(value);
    }

    get user$(): Observable<User> {
        return this._user.asObservable();
    }

    get Users$(): Observable<User[]> {
        return this._Users.asObservable();
    }

    get UsersInativos$(): Observable<User[]> {
        return this._UsersInativos.asObservable();
    }

    get allUsers$(): Observable<User[]> {
        return this._allUsers.asObservable();
    }

    get isAdmin$(): Observable<boolean> {
        return this.user$.pipe(
            map(user => user.role === 'admin')
        );
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Public methods
    // -----------------------------------------------------------------------------------------------------

    get(): Observable<User> {
        return this._httpClient.get<User>(this._baseUrl + '/me').pipe(
            tap((user) => {
                this._user.next(user);
            }),
        );
    }

    getAll(termo: string = ''): Observable<User[]> {
        let params = new HttpParams();
        if (termo.trim()) {
            params = params.set('termo', termo);
        }
        return this._httpClient.get<User[]>(this._baseUrl, { params }).pipe(
            tap((Users) => this._Users.next(Users))
        );
    }

    getAllInativos(termo: string = ''): Observable<User[]> {
        let params = new HttpParams();
        if (termo.trim()) {
            params = params.set('termo', termo);
        }
        return this._httpClient.get<User[]>(`${this._baseUrl}/inativos`, { params }).pipe(
            tap((Users) => this._UsersInativos.next(Users))
        );
    }

    getAllUsersCombined(): Observable<User[]> {
        return forkJoin({
            ativos: this.getAll(),
            inativos: this.getAllInativos()
        }).pipe(
            map(({ ativos, inativos }) => {
                const todosOsUsuarios = [...ativos, ...inativos];
                return todosOsUsuarios.sort((a, b) => (a.name || '').localeCompare(b.name || ''));
            }),
            
            tap(usuariosOrdenados => {
                this._allUsers.next(usuariosOrdenados);
            })
        );
    }

    getUserById(id: number): Observable<User | undefined> {
        return this._httpClient.get<User>(`${this._baseUrl}/${id}`);
    }

    // - Métodos de Escrita --

    update(user: User): Observable<User> {
        return this._httpClient.put<User>(`${this._baseUrl}/${user.id}`, user).pipe(
            switchMap(() => this.getAllUsersCombined()),
            map(() => user)
        );
    }

    delete(id: number): Observable<void> {
        return this._httpClient.delete<void>(`${this._baseUrl}/${id}`).pipe(
            tap(() => this.getAllUsersCombined().subscribe())
        );
    }

    reativar(user: User): Observable<User> {
        return this._httpClient.patch<User>(`${this._baseUrl}/reativar/${user.id}`, user).pipe(
            tap(() => this.getAllUsersCombined().subscribe())
        );
    }

    add(user: User): Observable<User> {
        return this._httpClient.post<User>(this._baseUrl, user).pipe(
            switchMap(() => this.getAllUsersCombined()),
            map(() => user)
        );
    }
}
