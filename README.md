# ClickEntrega

Sistema de gerenciamento de pedidos e entregas, conectando clientes a empresas com funcionalidades de rastreamento em tempo real.

## 🚀 Funcionalidades do Sistema

### 👤 Clientes
- **Cadastro e Login**: Acesso seguro com email e senha.
- **Catálogo de Empresas**: Visualização de empresas e seus produtos/cardápios.
- **Gestão de Pedidos**: Criação de novos pedidos e histórico completo.
- **Rastreamento em Tempo Real**: Acompanhamento do status do pedido (Aguardando Confirmação, Em Preparo, A Caminho, Entregue).
- **Notificações**: Recebimento de atualizações sobre o andamento do pedido.

### 🏢 Empresas
- **Gestão de Cardápio/Produtos**: Cadastro de produtos, preços e categorias.
- **Gestão de Pedidos**: Painel administrativo para aceitar, recusar e atualizar o status dos pedidos.
- **Estimativa de Entrega**: Definição de tempo estimado para entrega ao aceitar um pedido.
- **Gestão de Entregadores**: Cadastro e gerenciamento da frota de entregadores.

### 📦 Sistema de Entregas
- **Atualização de Status**: Fluxo completo de pedido (Pendente -> Em Preparo -> Saiu para Entrega -> Entregue).
- **Lógica de Atraso**: Monitoramento automático de pedidos que ultrapassaram a estimativa de entrega.

## 🛠️ Tecnologias Utilizadas
- **Backend**: .NET 8 (C#)
- **Banco de Dados**: PostgreSQL (Hospedado no Supabase)
- **ORM**: Entity Framework Core
- **Mensageria**: RabbitMQ (com fallback para simulação local)
- **Frontend**: HTML5, CSS3, JavaScript (Vanilla)

## 🔑 Credenciais de Teste

Utilize as credenciais abaixo para testar as funcionalidades do sistema:

### Conta de Cliente
- **Email:** `client1@test.com`
- **Senha:** `password123`

### Conta de Empresa
- **Nome:** `Test Company 1`
- **Senha:** `password123`

---

## ⚙️ Configuração Local

1.  **Pré-requisitos**:
    *   .NET 8 SDK instalado.
    *   (Opcional) RabbitMQ para funcionalidades avançadas de mensageria.

2.  **Como Executar**:
    ```bash
    dotnet restore
    dotnet run
    ```
    Acesse a aplicação em `http://localhost:5171` (ou a porta indicada no terminal).

O projeto está configurado para conectar-se automaticamente ao banco de dados na nuvem (Supabase).
Caso o RabbitMQ não esteja instalado localmente, o sistema utilizará automaticamente um serviço simulado (`FakeMessageBusService`) para garantir que a aplicação rode sem erros.
