#!/usr/bin/env bash
# Falha se um contrato OpenAPI tiver mudança que quebra em relação à versão publicada (doc 09, seção 2).
# Uso: scripts/contrato-breaking.sh <ref-base>   (ex.: origin/main)
set -euo pipefail

BASE_REF="${1:?informe a ref base, ex.: origin/main}"
TMP="$(mktemp -d)"
STATUS=0

for SPEC in contracts/openapi/*.yaml; do
  if ! git cat-file -e "${BASE_REF}:${SPEC}" 2>/dev/null; then
    echo "== ${SPEC}: sem versão publicada em ${BASE_REF}, nada a comparar."
    continue
  fi
  git show "${BASE_REF}:${SPEC}" > "${TMP}/base.yaml"
  echo "== ${SPEC}"
  oasdiff breaking "${TMP}/base.yaml" "${SPEC}" --fail-on ERR || STATUS=1
done

exit "${STATUS}"
