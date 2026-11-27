<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, type RouteLocationRaw } from 'vue-router'

/**
 * The one button of the site. Renders a RouterLink (`to`), a plain link (`href`)
 * or a <button>, so links and actions look the same.
 */
const props = withDefaults(
  defineProps<{
    /** primary: main action; accent: playful call-to-action; secondary: outlined; text: inline link style */
    variant?: 'primary' | 'accent' | 'secondary' | 'text'
    size?: 'sm' | 'md'
    /** Internal route; renders a RouterLink */
    to?: RouteLocationRaw
    /** External or anchor link; renders an <a> */
    href?: string
    /** Native button type when rendered as <button> */
    type?: 'button' | 'submit' | 'reset'
    disabled?: boolean
    /** Shows a busy state and blocks clicks, e.g. while a form submits */
    loading?: boolean
    /** Show the trailing arrow */
    arrow?: boolean
  }>(),
  {
    variant: 'primary',
    size: 'md',
    type: 'button',
    disabled: false,
    loading: false,
    arrow: false,
  },
)

const inactive = computed(() => props.disabled || props.loading)

// One element for all three cases; a disabled or loading link falls back to a disabled button
const element = computed(() => {
  if (props.to && !inactive.value) return { is: RouterLink, attrs: { to: props.to } }
  if (props.href && !inactive.value) return { is: 'a', attrs: { href: props.href } }
  return {
    is: 'button',
    attrs: { type: props.type, disabled: inactive.value, 'aria-busy': props.loading || undefined },
  }
})
</script>

<template>
  <component
    :is="element.is"
    v-bind="element.attrs"
    class="base-button"
    :class="{
      'base-button--primary': variant === 'primary',
      'base-button--accent': variant === 'accent',
      'base-button--secondary': variant === 'secondary',
      'base-button--text': variant === 'text',
      'base-button--md': size === 'md',
      'base-button--sm': size === 'sm',
      'is-inactive': inactive,
    }"
  >
    <span v-if="loading" class="base-button__spinner" aria-hidden="true"></span>
    <slot />
    <span v-if="arrow && !loading" class="base-button__arrow" aria-hidden="true">→</span>
  </component>
</template>

<style scoped>
.base-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-xs);
  border: 2px solid transparent;
  border-radius: var(--radius-pill);
  font-family: var(--font-body), sans-serif;
  font-weight: var(--weight-bold);
  line-height: 1.2;
  text-decoration: none;
  cursor: pointer;
  transition:
    transform var(--duration-fast) var(--ease-out),
    box-shadow var(--duration-fast) var(--ease-out),
    background-color var(--duration-normal) var(--ease-out),
    color var(--duration-normal) var(--ease-out);
}

.base-button--md {
  min-height: 48px;
  padding: 0 var(--space-md);
  font-size: var(--text-md);
}

.base-button--sm {
  min-height: 36px;
  padding: 0 var(--space-sm);
  font-size: var(--text-sm);
}

/* Primary and accent carry the hard offset shadow and "press in" on hover */
.base-button--primary,
.base-button--accent {
  box-shadow: var(--shadow-offset) var(--button-shadow);
}

.base-button--primary:hover,
.base-button--accent:hover {
  transform: translate(4px, 4px);
  box-shadow: var(--shadow-offset-pressed) var(--button-shadow);
}

/* Blue surface, so the shadow switches to terracotta */
.base-button--primary {
  --button-shadow: var(--color-terracotta);

  background: var(--color-blue);
  color: var(--color-white);
}

.base-button--primary:hover {
  color: var(--color-white);
}

.base-button--accent {
  --button-shadow: var(--shadow-color);

  background: var(--color-terracotta);
  color: var(--color-ink);
}

.base-button--accent:hover {
  color: var(--color-ink);
}

/* Secondary: outlined, fills on hover */
.base-button--secondary {
  border-color: var(--color-ink);
  background: transparent;
  color: var(--color-ink);
}

.base-button--secondary:hover {
  background: var(--color-ink);
  color: var(--color-white);
}

/* Text: inline link with an underline that grows */
.base-button--text {
  min-height: auto;
  padding: 0;
  border-radius: 0;
  background:
    linear-gradient(var(--color-terracotta), var(--color-terracotta)) left bottom / 0 2px
    no-repeat;
  color: var(--color-ink);
  transition: background-size var(--duration-normal) var(--ease-out);
}

.base-button--text:hover {
  background-size: 100% 2px;
  color: var(--color-ink);
}

.base-button__arrow {
  transition: transform var(--duration-normal) var(--ease-out);
}

.base-button:hover .base-button__arrow {
  transform: translateX(4px);
}

.base-button.is-inactive {
  opacity: 0.55;
  cursor: not-allowed;
  transform: none;
}

.base-button__spinner {
  width: 1em;
  height: 1em;
  border: 2px solid currentColor;
  border-right-color: transparent;
  border-radius: 50%;
  animation: base-button-spin 0.8s linear infinite;
}

@keyframes base-button-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
