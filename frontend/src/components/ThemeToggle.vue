<script setup lang="ts">
import { ref } from 'vue'

/**
 * Light/dark mode button, built like the language switch: one icon is visible and shows
 * the mode you switch to (half moon in light mode, sun in dark mode); a click swaps it.
 * TODO: only switches its own state so far; wire it to a real dark theme
 * (e.g. a data-theme attribute with dark token values) once that exists.
 */
const isDark = ref(false)
</script>

<template>
  <button
    type="button"
    class="theme-toggle"
    :aria-label="isDark ? 'Hellen Modus aktivieren' : 'Dunklen Modus aktivieren'"
    :aria-pressed="isDark"
    @click="isDark = !isDark"
  >
    <!-- Sun: shown in dark mode -->
    <svg v-if="isDark" class="theme-toggle__icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" />
    </svg>

    <!-- Half moon: shown in light mode -->
    <svg v-else class="theme-toggle__icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <path d="M20 14.5A8 8 0 1 1 9.5 4a6.5 6.5 0 0 0 10.5 10.5Z" />
    </svg>
  </button>
</template>

<style scoped>
/* Same look as the language switch: outlined circle in the surface colour, fills on hover */
.theme-toggle {
  display: grid;
  place-items: center;
  flex-shrink: 0;
  width: 36px;
  height: 36px;
  padding: 0;
  border: 2px solid var(--color-fg);
  border-radius: 50%;
  background: transparent;
  color: var(--color-fg);
  cursor: pointer;
  transition:
    background-color var(--duration-normal) var(--ease-out),
    color var(--duration-normal) var(--ease-out);
}

.theme-toggle:hover {
  background: var(--color-fg);
  color: var(--color-bg);
}

.theme-toggle__icon {
  width: 18px;
  height: 18px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
</style>
