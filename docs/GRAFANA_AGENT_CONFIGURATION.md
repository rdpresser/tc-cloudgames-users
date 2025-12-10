# ?? Grafana Agent Configuration Guide

## ?? Overview

A aplicação suporta export de **traces** para o **Grafana Cloud** através do **Grafana Agent**.

**Arquitetura:**
- **Logs:** Escritos em `stdout` (JSON) ? coletados pelo Grafana Agent ? enviados para Grafana Cloud Loki
- **Traces:** Gerados via OpenTelemetry ? exportados via OTLP ? Grafana Agent ? Grafana Cloud Tempo
- **Metrics:** Expostos no endpoint `/metrics` ? scraped pelo Grafana Agent ? enviados para Grafana Cloud Prometheus

**Comportamento:**
- ? Se `Grafana.Agent.Enabled = true` ? Traces são exportados via OTLP
- ?? Se `Grafana.Agent.Enabled = false` ? Traces são gerados mas **NÃO exportados**

---

## ?? Configuração

### **Opção 1: appsettings.json**

```json
{
  "Grafana": {
    "Agent": {
      "Host": "localhost",
      "OtlpGrpcPort": 4317,
      "OtlpHttpPort": 4318,
      "MetricsPort": 12345,
      "Enabled": true  // ? Controla export de traces
    },
    "Otlp": {
      "Endpoint": "http://localhost:4317",
      "Protocol": "grpc",
      "Headers": null,
      "TimeoutSeconds": 10,
      "Insecure": true
    }
  }
}
```

### **Opção 2: Environment Variables** (sobrescreve appsettings)

```sh
# Habilitar/Desabilitar Agent
GRAFANA_AGENT_ENABLED=true   # ou false

# Configurações do Agent
GRAFANA_AGENT_HOST=localhost
GRAFANA_AGENT_OTLP_GRPC_PORT=4317
GRAFANA_AGENT_OTLP_HTTP_PORT=4318
GRAFANA_AGENT_METRICS_PORT=12345

# OTLP Exporter
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_TIMEOUT=10
OTEL_EXPORTER_OTLP_INSECURE=true
```

---

## ?? Telemetry Stack

### **Logs**
- **Serilog** ? `stdout` (JSON) ? **Grafana Agent** ? **Grafana Cloud Loki**
- ?? **Independente** da flag `Agent.Enabled` (sempre escrito no stdout)

### **Traces**
- **OpenTelemetry** ? OTLP Exporter ? **Grafana Agent** ? **Grafana Cloud Tempo**
- ? **Depende** da flag `Agent.Enabled`
  - `true`: Exporta via OTLP
  - `false`: Gera traces localmente, mas **não exporta**

### **Metrics**
- **Prometheus** ? `/metrics` endpoint ? **Grafana Agent** (scrape) ? **Grafana Cloud Prometheus**
- ?? **Independente** da flag `Agent.Enabled` (endpoint sempre disponível)

---

## ?? Cenários de Uso

### **Scenario 1: Development Local (SEM Grafana Agent)**

```json
{
  "Grafana": {
    "Agent": {
      "Enabled": false  // ? Desabilita export
    }
  }
}
```

**Resultado:**
- ? Logs: escritos no console (stdout)
- ?? Traces: gerados, mas **não exportados**
- ? Metrics: disponíveis em `/metrics`

**Comportamento esperado:**
```
[WARN] Grafana Agent is DISABLED - Traces will be generated but NOT exported.
[WARN] To enable: Set Grafana:Agent:Enabled=true or GRAFANA_AGENT_ENABLED=true
```

**Quando usar:**
- Development local **sem Docker**
- Não quer rodar Grafana Agent localmente
- Apenas validando lógica da aplicação

---

### **Scenario 2: Development com Docker Compose**

```json
{
  "Grafana": {
    "Agent": {
      "Host": "localhost",
      "Enabled": true
    }
  }
}
```

**Resultado:**
- ? Logs: `stdout` ? Agent ? Grafana Cloud Loki
- ? Traces: OTLP ? Agent ? Grafana Cloud Tempo
- ? Metrics: `/metrics` ? Agent ? Grafana Cloud Prometheus

**docker-compose.yml:**
```yaml
services:
  grafana-agent:
    image: grafana/agent:latest
    ports:
      - "4317:4317"  # OTLP gRPC
      - "4318:4318"  # OTLP HTTP
      - "12345:12345" # Metrics
    volumes:
      - ./grafana-agent-config.yaml:/etc/agent/agent.yaml
```

---

### **Scenario 3: Production (AKS)**

```json
{
  "Grafana": {
    "Agent": {
      "Host": "grafana-agent.monitoring.svc.cluster.local",
      "Enabled": true
    }
  }
}
```

**Ou via Environment Variables:**
```sh
GRAFANA_AGENT_ENABLED=true
GRAFANA_AGENT_HOST=grafana-agent.monitoring.svc.cluster.local
```

**Resultado:**
- ? Logs: `stdout` ? Agent (DaemonSet) ? Grafana Cloud Loki
- ? Traces: OTLP ? Agent (DaemonSet) ? Grafana Cloud Tempo
- ? Metrics: `/metrics` ? Agent (scrape) ? Grafana Cloud Prometheus

---

## ?? Verificação

### **1. Verificar configuração na startup**

Procure por estas linhas no log da aplicação:

```
=== GRAFANA AGENT CONFIG ===
? Grafana Agent: ENABLED
   ? OTLP Export: ACTIVE
   ? Traces will be sent to Grafana Agent
   ? Logs: stdout ? Agent ? Grafana Cloud Loki
   ? Metrics: /metrics ? Agent scrape ? Grafana Cloud Prometheus
```

Ou, se desabilitado:

```
??  Grafana Agent: DISABLED
   ? OTLP Export: INACTIVE
   ? Traces will be generated but NOT exported
   ? Logs: stdout only (not sent to Grafana Cloud)
   ? Metrics: /metrics endpoint available (not scraped)
```

### **2. Testar conectividade com Agent**

```sh
# Verificar se Agent está rodando
curl http://localhost:12345/-/healthy

# Verificar se OTLP gRPC está acessível
grpcurl -plaintext localhost:4317 list
```

### **3. Verificar export de traces**

```sh
# Ver logs do Grafana Agent
docker logs grafana-agent

# Ou no Kubernetes
kubectl logs -n monitoring -l app=grafana-agent --tail=50 -f
```

---

## ?? Prioridade de Configuração

A configuração segue esta ordem de prioridade (maior para menor):

1. **Environment Variables** (ex: `GRAFANA_AGENT_ENABLED`)
2. **appsettings.{Environment}.json**
3. **appsettings.json**
4. **Defaults do código** (GrafanaOptions)

**Exemplo:**
```sh
# appsettings.json
"Grafana.Agent.Enabled": false

# Environment Variable (SOBRESCREVE)
GRAFANA_AGENT_ENABLED=true

# Resultado: Agent HABILITADO
```

---

## ?? Troubleshooting

### **Problema: Traces não aparecem no Grafana Cloud**

**Verificações:**

1. **Agent está habilitado?**
   ```sh
   # Ver log da aplicação
   grep "Grafana Agent" logs.txt
   # Deve mostrar: ? Grafana Agent: ENABLED
   ```

2. **Agent está rodando?**
   ```sh
   curl http://localhost:12345/-/healthy
   # Deve retornar: OK
   ```

3. **Aplicação consegue conectar no Agent?**
   ```sh
   # Ver logs da aplicação
   grep "OTLP Exporter configured" logs.txt
   ```

4. **Agent está enviando para Grafana Cloud?**
   ```sh
   # Ver logs do Agent
   docker logs grafana-agent | grep "trace"
   ```

### **Problema: Aplicação dá erro ao iniciar**

**Erro comum:**
```
Failed to connect to OTLP endpoint
```

**Solução:**
```sh
# Desabilite o Agent temporariamente
GRAFANA_AGENT_ENABLED=false

# Ou no appsettings.json
"Grafana.Agent.Enabled": false
```

---

## ?? Resumo

| Componente | Agent.Enabled=true | Agent.Enabled=false |
|------------|-------------------|---------------------|
| **Logs (stdout)** | ? Enviados | ? Escritos (não enviados) |
| **Traces (OTLP)** | ? Exportados | ?? Gerados (não exportados) |
| **Metrics (/metrics)** | ? Scraped | ? Disponíveis (não scraped) |

**Recomendação:**
- Development local: `Enabled = false`
- Development Docker: `Enabled = true`
- Production (AKS): `Enabled = true`

---

## ?? Links Úteis

- [Grafana Agent Documentation](https://grafana.com/docs/agent/latest/)
- [OpenTelemetry OTLP Specification](https://opentelemetry.io/docs/reference/specification/protocol/otlp/)
- [Grafana Cloud](https://grafana.com/products/cloud/)
