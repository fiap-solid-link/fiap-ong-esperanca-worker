# Esperanca Worker Service

Worker Service responsável por processar doações de forma assíncrona.

## Responsabilidade

Este serviço não expõe API HTTP. Ele fica rodando em background e faz o fluxo:

1. Consome `DoacaoRecebida` da fila `doacoes-recebidas`.
2. Verifica idempotência pelo `IdempotencyKey`.
3. Persiste a doação no MongoDB.
4. Publica `DoacaoProcessadaEvent` na fila `doacoes-processadas`.
5. Confirma a mensagem com ACK somente após processar com sucesso.

A atualização do `ValorArrecadado` da campanha continua sendo responsabilidade da Campanhas API, que consome `DoacaoProcessadaEvent`.

## Tecnologias

- .NET Worker Service
- RabbitMQ
- MongoDB
- Esperanca.Message

## Executar localmente

```bash
dotnet restore
dotnet build
dotnet run --project src/Esperanca.Worker.Service/Esperanca.Worker.Service.csproj
```

## Executar com Docker Compose

```bash
docker compose up --build
```

RabbitMQ Management:

```txt
http://localhost:15672
```

Usuário/senha padrão:

```txt
guest / guest
```

## Configurações principais

```json
{
  "ConnectionStrings": {
    "MongoDb": "mongodb://localhost:27017"
  },
  "Mongo": {
    "DatabaseName": "doacoes_db",
    "DoacoesCollection": "doacoes"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Exchange": "esperanca.doacoes",
    "RecebidaQueue": "doacoes-recebidas",
    "RecebidaRoutingKey": "recebida",
    "ProcessadaQueue": "doacoes-processadas",
    "ProcessadaRoutingKey": "processada"
  }
}
```

## Observação sobre integração

Se a Campanhas API e este Worker estiverem em `docker-compose` separados, ambos precisam apontar para o mesmo RabbitMQ. Em ambiente integrado, prefira subir RabbitMQ em apenas um compose e conectar os serviços na mesma network.
