import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, ReplaySubject } from 'rxjs';
import { RateTable } from './simulation.types';
import { EnvironmentInjector } from '@angular/core';
import { tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class RateTableService {

  private _apiUrl = 'http://localhost:5263/api/ratetables';

  private _tables$ = new ReplaySubject<RateTable[]>(1);

  constructor(private _http: HttpClient) {}

  getTables$(): Observable<RateTable[]> {
    return this._tables$.asObservable();
  }

  getTablesForSimulation(): Observable<RateTable[]> {
    return this._http.get<RateTable[]>(`${this._apiUrl}/simulation`).pipe(
      tap(tables => 
        this._tables$.next(tables))
    );
  }
}
