import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, ReplaySubject, tap, map, catchError, of, throwError } from 'rxjs';
import { User } from 'app/core/user/user.types';
import { AuthUtils } from './auth.utils';

// Interfaces para a requisição e resposta de login
export interface LoginRequest {
    email: string;
    password: string;
}

export interface LoginResponse {
    token: string;
    user: User;
}

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private _authenticated: boolean = false;
    private _user = new ReplaySubject<User | null>(1);
    private _currentUser: User | null = null;
    private _baseUrl = 'http://localhost:5263/api/auth';

    constructor(private _httpClient: HttpClient) { }

    // --- Acessores ---
    set user(value: User | null) {
        this._currentUser = value;
        this._user.next(value);
    }

    get user$(): Observable<User | null> {
        return this._user.asObservable();
    }

    get isAdmin$(): Observable<boolean> {
        return this.user$.pipe(map(user => !!user && user.role === 'admin'));
    }

    get accessToken(): string | null {
        return localStorage.getItem('accessToken');
    }

    private set accessToken(token: string | null) {
        if (token) {
            localStorage.setItem('accessToken', token);
        } else {
            localStorage.removeItem('accessToken');
        }
    }

    // --- Métodos ---
    check(): Observable<boolean> {
        if (this._authenticated) return of(true);
        if (!this.accessToken) return of(false);
        if (AuthUtils.isTokenExpired(this.accessToken)) {
            return this.signOut().pipe(map(() => false));
        }
        return this.signInUsingToken();
    }

    /**
     * Realiza o login e trata as mensagens de erro da API.
     */
    signIn(loginData: LoginRequest): Observable<LoginResponse> {
        return this._httpClient.post<LoginResponse>(`${this._baseUrl}/login`, loginData).pipe(
            tap((response) => {
                this.accessToken = response.token;
                this.user = response.user;
                this._authenticated = true;
            }),
            catchError((response: HttpErrorResponse) => {
                let errorMessage = 'Ocorreu um erro, por favor tente novamente.';

                if (typeof response.error === 'string') {
                    errorMessage = response.error;
                }

                return throwError(() => errorMessage);
            })
        );
    }

    signInUsingToken(): Observable<boolean> {
        return this._httpClient.get<User>(`${this._baseUrl}/me`).pipe(
            map((user) => {
                this.user = user;
                this._authenticated = true;
                return true;
            }),
            catchError(() => {
                this.signOut().subscribe();
                return of(false);
            })
        );
    }

    signOut(): Observable<any> {
        this.accessToken = null;
        this.user = null;
        this._authenticated = false;
        return of(true);
    }

    // --- Métodos Adicionais de Autenticação (Placeholders) ---
    signUp(user: any): Observable<any> {
        return this._httpClient.post(`${this._baseUrl}/sign-up`, user);
    }

    forgotPassword(email: string): Observable<any> 
    {
        return this._httpClient.post(`${this._baseUrl}/forgot-password`, { email });
    }

    resetPassword(token: string, newPassword: string): Observable<any> 
    {    
        return this._httpClient.post(`${this._baseUrl}/reset-password`, { token, newPassword });
    }

    unlockSession(credentials: LoginRequest): Observable<any> {
        return this._httpClient.post(`${this._baseUrl}/unlock-session`, credentials);
    }

    getCurrentUserName(): string {
        return this._currentUser?.name || 'Utilizador do Sistema';
    }
}
