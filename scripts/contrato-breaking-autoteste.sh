#!/usr/bin/env bash
# Prova negativa da checagem de compatibilidade (doc 09, seção 2):
# remove o campo `total` (lido pelo consumidor web) de uma cópia da OpenAPI v1
# e exige que o oasdiff acuse quebra. Se o oasdiff não acusar, a checagem não protege nada e o CI falha.
set -euo pipefail

TMP="$(mktemp -d)"
python3 - "$TMP/v1-quebrado.yaml" <<'PY'
import sys, yaml
spec = yaml.safe_load(open("contracts/openapi/orders-v1.yaml", encoding="utf-8"))
order = spec["components"]["schemas"]["Order"]
order["properties"].pop("total")
order["required"] = [c for c in order["required"] if c != "total"]
yaml.safe_dump(spec, open(sys.argv[1], "w", encoding="utf-8"), allow_unicode=True, sort_keys=False)
PY

if oasdiff breaking contracts/openapi/orders-v1.yaml "$TMP/v1-quebrado.yaml" --fail-on ERR; then
  echo "FALHA: remover 'total' do v1 deveria ser acusado como quebra de contrato."
  exit 1
fi
echo "OK: oasdiff acusou a quebra (campo 'total' removido do v1)."
