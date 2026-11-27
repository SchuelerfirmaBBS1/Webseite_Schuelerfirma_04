<script setup lang="ts">
/**
 * Slightly rotated label, like a sticker on a box. For short playful notes
 * ("Mit Ideen gemacht!"), at most one per section.
 */
withDefaults(
  defineProps<{
    tone?: 'terracotta' | 'blue' | 'cream'
    /** Rotation direction */
    tilt?: 'left' | 'right'
  }>(),
  { tone: 'terracotta', tilt: 'left' },
)
</script>

<template>
  <span class="base-sticker" :class="[`base-sticker--${tone}`, `base-sticker--${tilt}`]">
    <slot />
  </span>
</template>

<style scoped>
.base-sticker {
  display: inline-block;
  padding: var(--space-xs) var(--space-sm);
  border: var(--border);
  border-radius: var(--radius-sm);
  box-shadow: var(--shadow-offset) var(--sticker-shadow, var(--shadow-color));
  font-family: var(--font-display);
  font-size: var(--text-md);
  font-weight: var(--weight-bold);
  line-height: 1.2;
  white-space: nowrap;
  transition: transform var(--duration-normal) var(--ease-out);
}

.base-sticker--left {
  transform: rotate(-4deg);
}

.base-sticker--right {
  transform: rotate(3deg);
}

.base-sticker:hover {
  transform: rotate(0deg) scale(1.03);
}

.base-sticker--terracotta {
  background: var(--color-terracotta);
  color: var(--color-ink);
}

/* Blue surface, so the shadow switches to terracotta */
.base-sticker--blue {
  --sticker-shadow: var(--color-terracotta);

  background: var(--color-blue);
  color: var(--color-white);
}

.base-sticker--cream {
  background: var(--color-cream);
  color: var(--color-ink);
}
</style>
