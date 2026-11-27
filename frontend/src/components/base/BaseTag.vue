<script setup lang="ts">
/** Small pill label, e.g. for categories or dates. */
withDefaults(
  defineProps<{
    tone?: 'blue' | 'sage' | 'terracotta' | 'neutral'
  }>(),
  { tone: 'neutral' },
)
</script>

<template>
  <span class="base-tag" :class="`base-tag--${tone}`"><slot /></span>
</template>

<style scoped>
.base-tag {
  display: inline-flex;
  align-items: center;
  padding: 0 var(--space-xs);
  border-radius: var(--radius-pill);
  font-size: var(--text-xs);
  font-weight: var(--weight-bold);
  letter-spacing: 0.04em;
  line-height: 1.8;
}

.base-tag--neutral {
  background: var(--color-cream);
  color: var(--color-ink);
  box-shadow: inset 0 0 0 1px var(--color-ink);
}

.base-tag--blue {
  background: var(--color-blue);
  color: var(--color-white);
}

.base-tag--sage {
  background: var(--color-sage);
  color: var(--color-white);
}

.base-tag--terracotta {
  background: var(--color-terracotta);
  color: var(--color-ink);
}
</style>
