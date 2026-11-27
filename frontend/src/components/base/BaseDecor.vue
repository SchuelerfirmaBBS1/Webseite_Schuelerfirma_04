<script setup lang="ts">
/**
 * Small scattered decoration: a star (the ✳ of the front page), a cluster of dots
 * or a burst of rays. Purely decorative; position it from the parent.
 * Use sparingly, at most one per area.
 */
const props = withDefaults(
  defineProps<{
    kind?: 'star' | 'dots' | 'rays'
    color?: 'terracotta' | 'blue' | 'sage'
    /** Slow rotation, for the star only */
    spin?: boolean
  }>(),
  { kind: 'star', color: 'terracotta', spin: false },
)

// Star: eight thin arms around the centre, like the ✳ character
const star = Array.from({ length: 8 }, (_, i) => {
  const rad = (i * 45 * Math.PI) / 180
  return { x1: 40, y1: 40, x2: 40 + 34 * Math.cos(rad), y2: 40 + 34 * Math.sin(rad) }
})

const dots = [
  { cx: 12, cy: 14, r: 6 },
  { cx: 44, cy: 8, r: 3 },
  { cx: 34, cy: 36, r: 9 },
  { cx: 70, cy: 26, r: 4 },
  { cx: 62, cy: 62, r: 5 },
  { cx: 18, cy: 60, r: 3 },
]

// Rays fan out from the bottom-left corner
const rays = [-80, -60, -40, -20, 0].map((angle) => {
  const rad = (angle * Math.PI) / 180
  return {
    x1: 14 + 18 * Math.cos(rad),
    y1: 66 + 18 * Math.sin(rad),
    x2: 14 + 52 * Math.cos(rad),
    y2: 66 + 52 * Math.sin(rad),
  }
})

const lines = props.kind === 'rays' ? rays : star
</script>

<template>
  <svg
    class="base-decor"
    :class="{
      'base-decor--terracotta': color === 'terracotta',
      'base-decor--blue': color === 'blue',
      'base-decor--sage': color === 'sage',
      'base-decor--spin': spin && kind === 'star',
    }"
    viewBox="0 0 80 80"
    aria-hidden="true"
    focusable="false"
  >
    <template v-if="kind === 'dots'">
      <circle v-for="(dot, i) in dots" :key="i" v-bind="dot" class="base-decor__fill" />
    </template>
    <template v-else>
      <line v-for="(line, i) in lines" :key="i" v-bind="line" class="base-decor__stroke" />
    </template>
  </svg>
</template>

<style scoped>
.base-decor {
  width: 4rem;
  height: 4rem;
  overflow: visible;
  pointer-events: none;
}

.base-decor__fill {
  fill: var(--decor-color);
}

.base-decor__stroke {
  stroke: var(--decor-color);
  stroke-width: 4;
  stroke-linecap: round;
}

.base-decor--terracotta {
  --decor-color: var(--color-terracotta);
}

.base-decor--blue {
  --decor-color: var(--color-blue);
}

.base-decor--sage {
  --decor-color: var(--color-sage);
}

.base-decor--spin {
  animation: base-decor-spin 18s linear infinite;
}

@keyframes base-decor-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
