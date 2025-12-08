
export interface Installment {
    id: number;
    simulationId: number;
    installmentNumber: number;
    originalAmount: number;
    status: string;
    dueDate: string;
    paidAmount: number | null;
    paymentDate: number | null;

    openingBalance: number;
    interest: number;
    amortization: number;
    balance: number;
}

export interface PagedInstallmentResult {
    items: Installment[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
}