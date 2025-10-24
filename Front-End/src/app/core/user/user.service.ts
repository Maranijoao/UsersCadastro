import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, tap, forkJoin, map, ReplaySubject, Subject } from 'rxjs';
import { User } from './user.types';
import { RoleFilter, StatusFilter } from 'app/modules/clientes/sidebar/sidebar.component';

export interface ChartDataPoint {
    x: string;
    y: number;
}

export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
    roleFilter: string;
    statusFilter: string;
}

@Injectable({
    providedIn: 'root'
})
export class UserService {
    private _users = new BehaviorSubject<User[]>([]);
    private _pagination = new BehaviorSubject<PagedResult<any> | null>(null);
    private _user = new BehaviorSubject<User | null>(null);
    private _allUsers = new ReplaySubject<User[]>(1);
    private _listNeedsRefresh = new Subject<void>();
    private _totalUsersCount = new BehaviorSubject<number>(0);
    private _baseUrl = 'http://localhost:5263/api/users';

    constructor(private _httpClient: HttpClient) { }

    // --- Acessores ---

    get users$(): Observable<User[]> {
        return this._users.asObservable();
    }

    get user$(): Observable<User | null> {
        return this._user.asObservable();
    }

    get pagination$(): Observable<PagedResult<any> | null> {
        return this._pagination.asObservable();
    }

    get listNeedsRefresh$(): Observable<void> {
        return this._listNeedsRefresh.asObservable();
    }

    get allUsers$(): Observable<User[]> {
        return this._allUsers.asObservable();
    }

    get totalUsersCount$(): Observable<number> {
        return this._totalUsersCount.asObservable();
    }

    // --- Métodos Públicos ---

    notifyListChanged(): void {
        this._listNeedsRefresh.next();
    }

    getUserRegistrationsByDay(): Observable<ChartDataPoint[]> {
        return this._httpClient.get<ChartDataPoint[]>(`${this._baseUrl}/registrations-by-day`);
    }

    getAllUsersCombined(): Observable<User[]> {
        return forkJoin({
            users: this.getUsers()
        }).pipe(
            map(({ users }) => {
                const allUsers = [...users].sort((a, b) =>
                    (a.name || '').localeCompare(b.name || '')
                );
                this._allUsers.next(allUsers);
                return allUsers;
            })
        );
    }

    getUsers(term: string = '', pageNumber: number = 1, pageSize: number = 10, roleFilter: string = 'all', statusFilter: string = 'all'
    ): Observable<any> {
        let params = new HttpParams()
            .set('term', term)
            .set('pageNumber', pageNumber.toString())
            .set('pageSize', pageSize.toString())

        if (roleFilter !== 'all') {
            console.log(roleFilter);
            params = params.set('roleFilter', roleFilter);
        }

        if (statusFilter !== 'all') {
            const statusValue = statusFilter === 'active' ? 'true' : 'false';
            params = params.set('statusFilter', statusValue);
        }

        return this._httpClient.get<PagedResult<User>>(this._baseUrl, { params }).pipe(
            tap((pagedResult) => {
                this._totalUsersCount.next(pagedResult.totalCount);
                this._users.next(pagedResult.items);
                this._pagination.next(pagedResult);
            })
        );
    }

    getUserById(id: number): Observable<User> {
        return this._httpClient.get<User>(`${this._baseUrl}/${id}`);
    }

    add(user: User): Observable<User> {
        return this._httpClient.post<User>(this._baseUrl, user);
    }

    update(user: User): Observable<User> {
        return this._httpClient.put<User>(`${this._baseUrl}/${user.id}`, user);
    }

    inactivate(id: number): Observable<any> {
        return this._httpClient.delete(`${this._baseUrl}/${id}`);
    }

    reactivate(id: number): Observable<any> {
        return this._httpClient.post(`${this._baseUrl}/${id}/reactivate`, {});
    }
}

