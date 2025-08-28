import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { AuthUtils } from 'app/core/auth/auth.utils';
import { UserService } from 'app/core/user/user.service';
import { catchError, map, Observable, of, switchMap, tap, throwError } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private _authenticated: boolean = false;
    private _httpClient = inject(HttpClient);
    private _userService = inject(UserService);

    set accessToken(token: string) {
        localStorage.setItem('accessToken', token);
    }

    get accessToken(): string {
        return localStorage.getItem('accessToken') ?? '';
    }

    check(): Observable<boolean> {
        if (this._authenticated) {
            return of(true);
        }
        if (!this.accessToken) {
            return of(false);
        }
        if (AuthUtils.isTokenExpired(this.accessToken)) {
            this.signOut();
            return of(false);
        }
        return this.signInUsingToken();
    }

    signIn(credentials: { email: string; password: string }): Observable<any> {
        if (this._authenticated) {
            return throwError(() => 'O usuário já está logado.');
        }

        return this._httpClient.post('http://localhost:5263/api/Users/login', credentials).pipe(
            tap((response: any) => {
                this.accessToken = response.token;
                this._authenticated = true;
                this._userService.user = response.cliente;
            }),
            catchError((err: HttpErrorResponse) => {
                let errorMessage;
                if (err.status) {
                    console.log(err)
                    errorMessage = err.error || 'Email ou senha inválidos.';
                }
                return throwError(() => new Error(errorMessage));
            }),
        );
    }

    /**
     * Valida o token existente indo buscar os dados do utilizador.
     * Esta é a forma correta de restaurar uma sessão.
     */
    signInUsingToken(): Observable<boolean> {
        return this._userService.get().pipe(
            map((user) => {
                if (user) {
                    this._authenticated = true;
                    return true;
                }
                return false;
            }),
            catchError(() => {
                // Se a busca falhar (ex: token inválido), desloga o utilizador.
                this.signOut();
                return of(false);
            })
        );
    }

    signOut(): Observable<any> {
        localStorage.removeItem('accessToken');
        this._authenticated = false;
        this._userService.user = null;
        return of(true);
    }

    // -----------------------------------------------------------------------------------------------------
    // @ Métodos Auxiliares de Autenticação
    // -----------------------------------------------------------------------------------------------------

    /**
     * Regista um novo utilizador.
     */
    signUp(user: { name: string; email: string; password: string; company: string }): Observable<any> {
        return this._httpClient.post('api/auth/sign-up', user);
    }

    /**
     * Solicita a recuperação de password.
     */
    forgotPassword(email: string): Observable<any> {
        return this._httpClient.post('api/auth/forgot-password', email);
    }

    /**
     * Define uma nova password.
     */
    resetPassword(password: string): Observable<any> {
        return this._httpClient.post('api/auth/reset-password', password);
    }

    /**
     * Desbloqueia a sessão.
     */
    unlockSession(credentials: { email: string; password: string }): Observable<any> {
        return this._httpClient.post('api/auth/unlock-session', credentials);
    }
}
