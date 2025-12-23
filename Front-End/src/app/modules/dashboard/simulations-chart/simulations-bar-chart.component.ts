import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { NgApexchartsModule, ApexOptions, ChartComponent } from 'ng-apexcharts';
import { SimulationService } from 'app/core/simulation/simulation.service';

@Component({
    selector: 'app-simulations-bar-chart',
    standalone: true,
    imports: [
        CommonModule,
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatMenuModule,
        NgApexchartsModule
    ],
    templateUrl: './simulations-bar-chart.component.html'
})

export class SimulationsBarChartComponent implements OnInit {
    @ViewChild('chart') chart: ChartComponent;

    public chartOptions: ApexOptions;
    public isLoading: boolean = true;

    constructor(private _simulationService: SimulationService) { }

    ngOnInit(): void {
        this.fetchData();
    }

    fetchData(): void {
        this.isLoading = true;

        this._simulationService.getSimulations('', 1, 100).subscribe({
            next: (response: any) => {

                const items = response.items || response.content || [];
                this.processChartData(items);
                this.isLoading = false;
            },
            error: (err) => {
                console.error('Erro ao carregar dados do gráfico', err);
                this.isLoading = false;
            }
        });
    }

    processChartData(items: any[]): void {
        let volInssNovo = 0;
        let qtdInssNovo = 0;

        let volRefin = 0;
        let qtdRefin = 0;

        items.forEach(sim => {
            const product = (sim.product || '').trim().toLowerCase();
            const valor = sim.contractValue || 0;

            if (product.includes('novo') || product === 'inss - novo') {
                volInssNovo += valor;
                qtdInssNovo++;
            }
            else if (product.includes('refin')) {
                volRefin += valor;
                qtdRefin++;
            }
        });

        console.log('Dados Processados para Gráfico:', {
            INSS_Novo: { qtd: qtdInssNovo, vol: volInssNovo },
            Refin: { qtd: qtdRefin, vol: volRefin }
        });

        const chartData = {
            categories: ['INSS - Novo', 'Refinanciamento'],
            series: [
                {
                    name: 'Volume (R$)',
                    data: [volInssNovo, volRefin]
                },
                {
                    name: 'Qtd. Contratos',
                    data: [qtdInssNovo, qtdRefin]
                }
            ]
        };

        this.initChart(chartData);
    }

    initChart(data: any): void {
        this.chartOptions = {
            series: data.series,
            chart: {
                type: 'bar',
                height: 350,
                fontFamily: 'inherit',
                toolbar: { show: false },
                animations: { enabled: true }
            },
            plotOptions: {
                bar: {
                    horizontal: false,
                    columnWidth: '40%',
                    borderRadius: 4,
                    dataLabels: {
                        position: 'top'
                    }
                },
            },
            dataLabels: {
                enabled: false
            },
            stroke: {
                show: true,
                width: 2,
                colors: ['transparent']
            },
            xaxis: {
                categories: data.categories,
                axisBorder: { show: false },
                axisTicks: { show: false },
                labels: {
                    style: { colors: '#64748b' }
                }
            },
            yaxis: [
                {
                    seriesName: 'Volume (R$)',
                    min: 0,
                    forceNiceScale: true,
                    labels: {
                        style: { colors: '#BDBDBD' },
                        formatter: (value) => {
                            return value >= 1000 ? `R$ ${(value / 1000).toFixed(0)}k` : value.toString();
                        }
                    },
                    title: {
                        text: "Volume Financeiro",
                        style: { color: '#EEEEEE' }
                    }
                },
                {
                    opposite: true,
                    seriesName: 'Qtd. Contratos',
                    min: 0,
                    forceNiceScale: true,
                    decimalsInFloat: 0,
                    labels: {
                        style: { colors: '#BDBDBD' } 
                    },
                    title: {
                        text: "Quantidade",
                        style: { color: '#EEEEEE' }
                    }
                }
            ],
            fill: {
                opacity: 1
            },
            tooltip: {
                shared: true,
                intersect: false,
                y: {
                    formatter: function (val, { seriesIndex }) {
                        if (seriesIndex === 0) return "R$ " + val.toLocaleString('pt-BR', { minimumFractionDigits: 2 });
                        return val.toFixed(0) + " un.";
                    }
                },
                theme: 'dark'
            },
            colors: ['#2563eb', '#93c5fd'],
            grid: {
                borderColor: '#e2e8f0',
                strokeDashArray: 4,
                xaxis: { lines: { show: false } }
            },
            legend: {
                position: 'top',
                horizontalAlign: 'right',
                offsetY: -20
            }
        };
    }
}