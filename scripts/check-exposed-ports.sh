#!/usr/bin/env bash
#
# Fails if the compose setup publishes a port that would be reachable from outside, other than
# 80/443 of the "proxy" service. Docker's published ports bypass ufw/firewalld, so this has to be
# right in the compose file itself.
#
# Rules (evaluated on the normalised output of "docker compose config"):
#   - "proxy" may publish 80 and 443 (tcp/udp) on all interfaces,
#   - every other published port must be bound to loopback (127.0.0.1 or ::1),
#   - no service may use network_mode "host" (it would expose every listening port).
#
# Usage:
#   scripts/check-exposed-ports.sh [--file deploy/compose.yaml] [--env-file deploy/.env]
#   scripts/check-exposed-ports.sh --config-json config.json   # already rendered config (tests)
#
# Needs jq and Docker Compose v2 or newer ("docker compose" or "docker-compose"; override with
# COMPOSE="..."). podman-compose does not normalise the configuration and is not supported.

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
compose_file="$repo_root/deploy/compose.yaml"
env_file=""
config_json=""

usage() {
    sed -n '/^# Usage:/,/^# Needs/p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --file) compose_file="$2"; shift 2 ;;
        --env-file) env_file="$2"; shift 2 ;;
        --config-json) config_json="$2"; shift 2 ;;
        -h | --help) usage; exit 0 ;;
        *) echo "Unknown argument: $1" >&2; usage >&2; exit 2 ;;
    esac
done

command -v jq >/dev/null || { echo "jq is required." >&2; exit 2; }

render_config() {
    local -a compose
    if [[ -n "${COMPOSE:-}" ]]; then
        read -r -a compose <<<"$COMPOSE"
    elif docker compose version >/dev/null 2>&1; then
        compose=(docker compose)
    elif command -v docker-compose >/dev/null; then
        compose=(docker-compose)
    else
        echo "Docker Compose v2 is required (\"docker compose\" or \"docker-compose\")." >&2
        exit 2
    fi

    local -a args=(-f "$compose_file")
    [[ -n "$env_file" ]] && args+=(--env-file "$env_file")
    "${compose[@]}" "${args[@]}" config --format json
}

if [[ -n "$config_json" ]]; then
    config="$(cat "$config_json")"
else
    config="$(render_config)"
fi

# One line per violation. Anything that is not in the normalised long form counts as a violation,
# so an unexpected format can never slip through.
violations="$(jq -r '
    def loopback: . == "127.0.0.1" or . == "::1";
    (.services // {}) | to_entries[] | .key as $service | .value as $config
    | (
        if ($config.network_mode // "") == "host"
        then "\($service): network_mode \"host\" exposes every listening port of the container"
        else empty end
      ),
      (
        ($config.ports // [])[]
        | if type != "object" then
            "\($service): port \(tojson) is not normalised (render the file with Docker Compose v2)"
          else
            (.host_ip // "") as $ip
            | (.published // "" | tostring) as $published
            | if ($ip | loopback) then empty
              elif $service == "proxy" and ($published == "80" or $published == "443") then empty
              else
                "\($service): port \(if $published == "" then "(random)" else $published end) -> \(.target)/\(.protocol // "tcp") is published on \(if $ip == "" then "all interfaces" else $ip end); bind it to 127.0.0.1 or remove it"
              end
          end
      )
' <<<"$config")"

if [[ -n "$violations" ]]; then
    echo "Ports reachable from outside:" >&2
    sed 's/^/  - /' <<<"$violations" >&2
    echo "Only \"proxy\" may publish 80/443; everything else needs no \"ports:\" (use \"expose:\") or a 127.0.0.1 prefix." >&2
    exit 1
fi

echo "OK: only the proxy publishes 80/443, all other published ports are bound to loopback."
echo "After starting, also check on the server (ss -tlnp) and from another machine (e.g. nmap -Pn <server>)."
