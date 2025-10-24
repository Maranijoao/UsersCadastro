export interface Address {
  id: number;
  userId: number;
  cep: string;
  street: string;
  number: string;
  complement?: string
  neighborhood: string;
  city: string;
  state: string;
}

export interface UserLog {
  id: number;
  userId: number;
  changedAt: string;
  changedBy: string;
  action: string;
  oldValues?: string;
  newValues?: string;
}

export interface User {
  id: number; // antes estava como string
  name: string
  cpf: string;
  email: string;
  phoneNumber: string;
  password: string;
  role?: string;
  recordStatus?: boolean; // indica se o usuário está ativo (true) ou inativo
  address: Address[];
  avatar?: string; //
  status?: string; //
  birthDate?: string;
  createdAt?: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
  inactivatedAt?: string;
  inactivatedBy?: string;
  logs: UserLog[];
}