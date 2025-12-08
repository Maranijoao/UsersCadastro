import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Installment, PagedInstallmentResult } from './installment.types';

@Injectable({
    providedIn: 'root'
})
export class InstallmentService {

    private _apiUrl = 'http://localhost:5263/api/installment';

    constructor(private _http: HttpClient) { }

    getInstallmentsForSimulation(
        simulationId: number,
        page: number = 1,
        pageSize: number = 10
    ): Observable<PagedInstallmentResult> {

        let params = new HttpParams()
            .set('pageNumber', page.toString())
            .set('pageSize', pageSize.toString());

        return this._http.get<PagedInstallmentResult>(`${this._apiUrl}/simulation/${simulationId}`, { params });
    }

    payInstallment(id: number, amount: number, paymentDate: Date): Observable<any> {
        return this._http.post(`${this._apiUrl}/pay/${id}`,
            {
                amount: amount,
                paymentDate: paymentDate
            });
    }
}