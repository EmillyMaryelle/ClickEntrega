# Configuração do RabbitMQ

Este projeto está configurado para usar RabbitMQ para processamento assíncrono de notificações.

## Instalação do RabbitMQ

### Opção 1: Docker (Recomendado)

Execute o seguinte comando para iniciar o RabbitMQ:

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Isso iniciará o RabbitMQ com:
- Porta 5672: Para conexões AMQP
- Porta 15672: Interface de gerenciamento web (http://localhost:15672)
- Usuário padrão: `guest`
- Senha padrão: `guest`

### Opção 2: Instalação Local

1. Baixe o RabbitMQ em: https://www.rabbitmq.com/download.html
2. Siga as instruções de instalação para seu sistema operacional
3. Inicie o serviço RabbitMQ

## Configuração

As configurações do RabbitMQ podem ser definidas de duas formas:

### 1. Variáveis de Ambiente (Recomendado para produção)

```bash
RABBITMQ_HOST=localhost
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_PORT=5672
```

### 2. appsettings.json (Desenvolvimento)

O arquivo `appsettings.json` já contém as configurações padrão:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "Port": 5672
  }
}
```

## Como Funciona

### Fluxo de Notificações

1. **Quando um pedido é criado ou atualizado:**
   - O `OrdersController` publica uma mensagem na fila `notifications`
   - A mensagem contém: OrderId, ClientId, Message, OrderStatus

2. **Processamento Assíncrono:**
   - O `NotificationConsumerService` (Background Service) consome as mensagens
   - Cria registros na tabela `Notification` do banco de dados
   - Processa uma mensagem por vez para garantir ordem

3. **Cliente recebe notificação:**
   - O frontend consulta `/api/Notifications/Client/{clientId}` periodicamente
   - As notificações aparecem no modal de notificações

### Filas Criadas

- **notifications**: Fila para notificações de pedidos
- **order_status**: Fila para mudanças de status (pode ser usado para analytics futuros)

## Verificando o RabbitMQ

### Interface Web

Acesse: http://localhost:15672

- Login: `guest`
- Senha: `guest`

Na interface você pode:
- Ver filas e mensagens
- Monitorar conexões
- Ver estatísticas de mensagens

### Verificar se está funcionando

1. Crie um pedido na aplicação
2. Acesse a interface do RabbitMQ
3. Vá em "Queues" e verifique a fila `notifications`
4. Você verá mensagens sendo processadas

## Troubleshooting

### Erro: "Connection refused"

- Verifique se o RabbitMQ está rodando
- Verifique se a porta 5672 está acessível
- Verifique as configurações de host/porta

### Mensagens não estão sendo processadas

- Verifique os logs da aplicação
- Verifique se o `NotificationConsumerService` está registrado no `Program.cs`
- Verifique se há erros no banco de dados

### Aplicação funciona sem RabbitMQ?

Sim! A aplicação foi projetada para funcionar mesmo se o RabbitMQ não estiver disponível. As notificações simplesmente não serão enviadas, mas a aplicação continuará funcionando normalmente.

## Próximos Passos

- [ ] Adicionar Dead Letter Queue para mensagens com erro
- [ ] Implementar retry automático
- [ ] Adicionar métricas e monitoramento
- [ ] Implementar notificações em tempo real com SignalR

