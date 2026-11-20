#!/usr/bin/env bash
#
# Tests for scripts/check-exposed-ports.sh. The fixtures have the normalised form that
# "docker compose config --format json" produces, so no Docker is needed.
#
#   scripts/tests/check-exposed-ports.test.sh

set -uo pipefail

script="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/check-exposed-ports.sh"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

failures=0

# expect <name> <expected exit code> <expected text or ""> <config json>
expect() {
    local name="$1" expected_code="$2" expected_text="$3" json="$4"
    printf '%s' "$json" >"$work/config.json"

    local output code
    output="$("$script" --config-json "$work/config.json" 2>&1)"
    code=$?

    if [[ "$code" -ne "$expected_code" ]]; then
        echo "FAIL $name: exit code $code, expected $expected_code"
        sed 's/^/     /' <<<"$output"
        failures=$((failures + 1))
    elif [[ -n "$expected_text" && "$output" != *"$expected_text"* ]]; then
        echo "FAIL $name: output does not contain \"$expected_text\""
        sed 's/^/     /' <<<"$output"
        failures=$((failures + 1))
    else
        echo "ok   $name"
    fi
}

proxy_ports='[
  {"mode":"ingress","target":80,"published":"80","protocol":"tcp"},
  {"mode":"ingress","target":443,"published":"443","protocol":"tcp"},
  {"mode":"ingress","target":443,"published":"443","protocol":"udp"}
]'

expect "proxy on 80/443 and services without ports pass" 0 "OK" "{\"services\":{
  \"proxy\":{\"ports\":$proxy_ports},
  \"api\":{\"expose\":[\"8080\"]},
  \"db\":{}
}}"

expect "db published as 3306:3306 fails" 1 "db: port 3306 -> 3306/tcp is published on all interfaces" "{\"services\":{
  \"proxy\":{\"ports\":$proxy_ports},
  \"db\":{\"ports\":[{\"mode\":\"ingress\",\"target\":3306,\"published\":\"3306\",\"protocol\":\"tcp\"}]}
}}"

expect "api bound to 0.0.0.0 fails" 1 "api: port 8080 -> 8080/tcp is published on 0.0.0.0" '{"services":{
  "api":{"ports":[{"mode":"ingress","host_ip":"0.0.0.0","target":8080,"published":"8080","protocol":"tcp"}]}
}}'

expect "api bound to 127.0.0.1 passes (local debugging)" 0 "OK" '{"services":{
  "api":{"ports":[{"mode":"ingress","host_ip":"127.0.0.1","target":8080,"published":"8080","protocol":"tcp"}]}
}}'

expect "db bound to ::1 passes" 0 "OK" '{"services":{
  "db":{"ports":[{"mode":"ingress","host_ip":"::1","target":3306,"published":"3306","protocol":"tcp"}]}
}}'

expect "api with a random host port fails" 1 "api: port (random) -> 8080/tcp" '{"services":{
  "api":{"ports":[{"mode":"ingress","target":8080,"protocol":"tcp"}]}
}}'

expect "proxy publishing another port fails" 1 "proxy: port 2019 -> 2019/tcp" "{\"services\":{
  \"proxy\":{\"ports\":$(jq -c '. + [{"mode":"ingress","target":2019,"published":"2019","protocol":"tcp"}]' <<<"$proxy_ports")}
}}"

expect "another service on 443 fails" 1 "web: port 443 -> 443/tcp" '{"services":{
  "web":{"ports":[{"mode":"ingress","target":443,"published":"443","protocol":"tcp"}]}
}}'

expect "network_mode host fails" 1 "db: network_mode \"host\"" '{"services":{
  "db":{"network_mode":"host"}
}}'

expect "short port syntax fails closed" 1 "not normalised" '{"services":{
  "db":{"ports":["127.0.0.1:3306:3306"]}
}}'

expect "all violations are reported" 1 "api: port 8080" '{"services":{
  "db":{"ports":[{"mode":"ingress","target":3306,"published":"3306","protocol":"tcp"}]},
  "api":{"ports":[{"mode":"ingress","target":8080,"published":"8080","protocol":"tcp"}]}
}}'

if [[ "$failures" -gt 0 ]]; then
    echo "$failures test(s) failed."
    exit 1
fi
echo "All tests passed."
