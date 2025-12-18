# 📊 Grafana Agent Configuration Guide

## 🔍 Overview

The application supports **trace** export to **Grafana Cloud** through the **Grafana Agent**.

**Architecture:**
- **Logs:** Written to `stdout` (JSON) → collected by Grafana Agent → sent to Grafana Cloud Loki
- **Traces:** Generated via OpenTelemetry → exported via OTLP → Grafana Agent → Grafana Cloud Tempo
- **Metrics:** Exposed on `/metrics` endpoint → scraped by Grafana Agent → sent to Grafana Cloud Prometheus

**Behavior:**
- ✅ If `Grafana.Agent.Enabled = true` → Traces are exported via OTLP
- ❌ If `Grafana.Agent.Enabled = false` → Traces are generated but **NOT exported**

---

## ⚙️ Configuration

### **Option 1: appsettings.json**

```json
{
  "Grafana": {
    "Agent": {
      "Host": "localhost",
      "OtlpGrpcPort": 4317,
      "OtlpHttpPort": 4318,
      "MetricsPort": 12345,
      "Enabled": true  // ✅ Controls trace export
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

### **Option 2: Environment Variables** (overrides appsettings)

```sh
# Enable/Disable Agent
GRAFANA_AGENT_ENABLED=true   # or false

# Agent Configuration
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

## 📡 Telemetry Stack

### **Logs**
- **Serilog** → `stdout` (JSON) → **Grafana Agent** → **Grafana Cloud Loki**
- ✅ **Independent** of `Agent.Enabled` flag (always written to stdout)

### **Traces**
- **OpenTelemetry** → OTLP Exporter → **Grafana Agent** → **Grafana Cloud Tempo**
- ⚠️ **Depends** on `Agent.Enabled` flag
  - `true`: Exports via OTLP
  - `false`: Generates traces locally, but **does not export**

### **Metrics**
- **Prometheus** → `/metrics` endpoint → **Grafana Agent** (scrape) → **Grafana Cloud Prometheus**
- ✅ **Independent** of `Agent.Enabled` flag (endpoint always available)

---

## 🎯 Usage Scenarios

### **Scenario 1: Local Development (WITHOUT Grafana Agent)**

```json
{
  "Grafana": {
    "Agent": {
      "Enabled": false  // ❌ Disables export
    }
  }
}
```

**Result:**
- ✅ Logs: written to console (stdout)
- ⚠️ Traces: generated, but **not exported**
- ✅ Metrics: available at `/metrics`

**Expected behavior:**
```
[WARN] Grafana Agent is DISABLED - Traces will be generated but NOT exported.
[WARN] To enable: Set Grafana:Agent:Enabled=true or GRAFANA_AGENT_ENABLED=true
```

**When to use:**
- Local development **without Docker**
- Don't want to run Grafana Agent locally
- Only validating application logic

---

### **Scenario 2: Development with Docker Compose**

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

**Result:**
- ✅ Logs: `stdout` → Agent → Grafana Cloud Loki
- ✅ Traces: OTLP → Agent → Grafana Cloud Tempo
- ✅ Metrics: `/metrics` → Agent → Grafana Cloud Prometheus

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

**Or via Environment Variables:**
```sh
GRAFANA_AGENT_ENABLED=true
GRAFANA_AGENT_HOST=grafana-agent.monitoring.svc.cluster.local
```

**Result:**
- ✅ Logs: `stdout` → Agent (DaemonSet) → Grafana Cloud Loki
- ✅ Traces: OTLP → Agent (DaemonSet) → Grafana Cloud Tempo
- ✅ Metrics: `/metrics` → Agent (scrape) → Grafana Cloud Prometheus

---

## ✅ Verification

### **1. Check configuration on startup**

Look for these lines in the application log:

```
=== GRAFANA AGENT CONFIG ===
✅ Grafana Agent: ENABLED
   ✅ OTLP Export: ACTIVE
   ✅ Traces will be sent to Grafana Agent
   ✅ Logs: stdout → Agent → Grafana Cloud Loki
   ✅ Metrics: /metrics → Agent scrape → Grafana Cloud Prometheus
```

Or, if disabled:

```
❌  Grafana Agent: DISABLED
   ⚠️ OTLP Export: INACTIVE
   ⚠️ Traces will be generated but NOT exported
   ⚠️ Logs: stdout only (not sent to Grafana Cloud)
   ⚠️ Metrics: /metrics endpoint available (not scraped)
```

### **2. Test Agent connectivity**

```sh
# Check if Agent is running
curl http://localhost:12345/-/healthy

# Check if OTLP gRPC is accessible
grpcurl -plaintext localhost:4317 list
```

### **3. Verify trace export**

```sh
# View Grafana Agent logs
docker logs grafana-agent

# Ou no Kubernetes
kubectl logs -n monitoring -l app=grafana-agent --tail=50 -f
```

---

## 🔧 Configuration Priority

Configuration follows this priority order (highest to lowest):

1. **Environment Variables** (e.g., `GRAFANA_AGENT_ENABLED`)
2. **appsettings.{Environment}.json**
3. **appsettings.json**
4. **Code defaults** (GrafanaOptions)

**Example:**
```sh
# appsettings.json
"Grafana.Agent.Enabled": false

# Environment Variable (OVERRIDES)
GRAFANA_AGENT_ENABLED=true

# Result: Agent ENABLED
```

---

## 🔧 Troubleshooting

### **Problem: Traces don't appear in Grafana Cloud**

**Checks:**

1. **Is Agent enabled?**
   ```sh
   # View application log
   grep "Grafana Agent" logs.txt
   # Should show: ✅ Grafana Agent: ENABLED
   ```

2. **Is Agent running?**
   ```sh
   curl http://localhost:12345/-/healthy
   # Should return: OK
   ```

3. **Can application connect to Agent?**
   ```sh
   # View application logs
   grep "OTLP Exporter configured" logs.txt
   ```

4. **Is Agent sending to Grafana Cloud?**
   ```sh
   # View Agent logs
   docker logs grafana-agent | grep "trace"
   ```

### **Problem: Application fails to start**

**Common error:**
```
Failed to connect to OTLP endpoint
```

**Solution:**
```sh
# Disable Agent temporarily
GRAFANA_AGENT_ENABLED=false

# Ou no appsettings.json
"Grafana.Agent.Enabled": false
```

---

## 📝 Summary

| Component | Agent.Enabled=true | Agent.Enabled=false |
|------------|-------------------|---------------------|
| **Logs (stdout)** | ✅ Sent | ⚠️ Written (not sent) |
| **Traces (OTLP)** | ✅ Exported | ⚠️ Generated (not exported) |
| **Metrics (/metrics)** | ✅ Scraped | ⚠️ Available (not scraped) |

**Recommendation:**
- Development local: `Enabled = false`
- Development Docker: `Enabled = true`
- Production (AKS): `Enabled = true`

---

## 🔗 Useful Links

- [Grafana Agent Documentation](https://grafana.com/docs/agent/latest/)
- [OpenTelemetry OTLP Specification](https://opentelemetry.io/docs/reference/specification/protocol/otlp/)
- [Grafana Cloud](https://grafana.com/products/cloud/)
