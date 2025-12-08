import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, tap, forkJoin, map, ReplaySubject, Subject } from 'rxjs';
import { SimulationInput, SimulationResult, Simulation, PagedSimulationResult, DashboardTotals } from './simulation.types';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class SimulationService {

  private _apiUrl = 'http://localhost:5263/api/simulations'; //atenção{
  private _listNeedsRefresh$ = new ReplaySubject<void>(1);
  private _pagination = new BehaviorSubject<PagedSimulationResult | null>(null);

  constructor(private _http: HttpClient) { }

  get listNeedsRefresh$(): Observable<void> {
    return this._listNeedsRefresh$.asObservable();
  }

  notifyListChanged(): void {
    this._listNeedsRefresh$.next();
  }

  get pagination$(): Observable<PagedSimulationResult | null> {
    return this._pagination.asObservable();
  }

  calculate(input: SimulationInput): Observable<SimulationResult> {
    return this._http.post<SimulationResult>(`${this._apiUrl}/calculate`, input);
  }

  confirm(result: SimulationResult): Observable<Simulation> {
    console.log('Chamando API para confirmar simulação...', result);
    console.log('Enviando POST para:', `${this._apiUrl}/confirm`); 
    return this._http.post<Simulation>(`${this._apiUrl}/confirm`, result).pipe(
      tap((res) => {
        console.log('Resposta Confirm:', res);
        this.notifyListChanged();
      })
    );
  }

  getSimulations(term: string = '', page: number = 1, pageSize: number = 10): Observable<PagedSimulationResult> {
    let params = new HttpParams()
      .set('term', term)
      .set('pageNumber', page.toString())
      .set('pageSize', pageSize.toString());

    return this._http.get<PagedSimulationResult>(this._apiUrl, { params }).pipe(
      tap((pagedResult) => {
        this._pagination.next(pagedResult);
      })
    );
  }

  getSimulationById(id: number): Observable<Simulation> {
    return this._http.get<Simulation>(`${this._apiUrl}/${id}`);
  }

  getSimulationTotals(): Observable<DashboardTotals> {
    return this._http.get<DashboardTotals>(`${this._apiUrl}/totals`);
  }

  updateSimulation(id: number, simulationData: Simulation): Observable<any> {
    return this._http.put<Simulation>(`${this._apiUrl}/${id}`, simulationData).pipe(
      tap(() => {
        this.notifyListChanged();
      })
    );
  }
}
