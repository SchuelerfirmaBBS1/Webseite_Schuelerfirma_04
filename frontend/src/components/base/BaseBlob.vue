<script setup lang="ts">
/**
 * Decorative organic shape, placed behind images or text.
 * Position it from the parent (e.g. absolute with inset values); it never takes clicks.
 * cream is meant for white sections, white for cream sections.
 */
withDefaults(
  defineProps<{
    color?: 'cream' | 'white' | 'sage' | 'terracotta' | 'blue'
    /** Second, offset shape behind it in the shadow colour (blue, terracotta on dark sections) */
    offset?: boolean
    /** One of two outlines, so neighbouring blobs don't look identical */
    shape?: 1 | 2
  }>(),
  { color: 'cream', offset: false, shape: 1 },
)

const paths = {
  1: 'M421 287c-20 92-118 152-214 135C111 405 31 330 41 233 52 135 112 49 214 41c101-8 228 54 207 246Z',
  2: 'M407 168c35 82 4 196-82 236-87 40-210 12-262-68-51-80-26-190 49-252 76-61 260-1 295 84Z',
} as const
</script>

<template>
  <svg class="base-blob" :class="`base-blob--${color}`" viewBox="0 0 460 460" aria-hidden="true" focusable="false">
    <path v-if="offset" class="base-blob__offset" :d="paths[shape]" transform="translate(14 14)" />
    <path class="base-blob__shape" :d="paths[shape]" />
  </svg>
</template>

<style scoped>
.base-blob {
  width: 100%;
  height: auto;
  overflow: visible;
  pointer-events: none;
}

.base-blob__offset {
  fill: var(--shadow-color);
}

.base-blob--cream .base-blob__shape {
  fill: var(--color-cream);
}

.base-blob--white .base-blob__shape {
  fill: var(--color-white);
}

.base-blob--sage .base-blob__shape {
  fill: var(--color-sage);
}

.base-blob--terracotta .base-blob__shape {
  fill: var(--color-terracotta);
}

.base-blob--blue .base-blob__shape {
  fill: var(--color-blue);
}
</style>
