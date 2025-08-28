export interface Endereco {
  id: number; 
  clienteId: number; 
  cep: string; 
  logradouro: string; 
  numero: string; 
  complemento?: string; 
  bairro: string; 
  cidade: string; 
  uf: string; 
}

export interface ClienteLog{
  id: number;
  clienteId: number;
  dataAlteracao: string;
  usuarioAlteracao: string;
  acao: string;
}

export interface User {
  id: number; // antes estava como string
  name: string;
  cpf: string;
  email: string;
  telefone: string;
  password: string;
  role?: string; 
  recordStatus?: boolean; // indica se o usuário está ativo (true) ou inativo
  enderecos: Endereco[];
  avatar?: string; //
  status?: string; // 
  
  dataCadastro?: string;
  usuarioCadastro?: string;
  dataUltimaAlteracao?: string;
  usuarioUltimaAlteracao?: string;
  dataInativacao?: string;
  usuarioInativacao?: string;

  logs: ClienteLog[];
}
