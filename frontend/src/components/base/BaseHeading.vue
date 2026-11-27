<script setup lang="ts">
/**
 * Section or page heading: small uppercase eyebrow, a title with an optional
 * highlighted second part, and an optional lead text (default slot).
 */
withDefaults(
  defineProps<{
    eyebrow?: string
    title: string
    /** Second part of the title, set in the highlight colour on its own line */
    highlight?: string
    /** h1 for the page title, h2 for sections */
    level?: 1 | 2 | 3
    align?: 'start' | 'center'
    /** Use on dark sections */
    inverse?: boolean
  }>(),
  { level: 2, align: 'start', inverse: false },
)
</script>

<template>
  <div
    class="base-heading"
    :class="{ 'base-heading--center': align === 'center', 'base-heading--inverse': inverse }"
  >
    <p v-if="eyebrow" class="base-heading__eyebrow">{{ eyebrow }}</p>

    <component :is="`h${level}`" class="base-heading__title" :class="{
        'base-heading__title--h1': level === 1,
        'base-heading__title--h2': level === 2,
        'base-heading__title--h3': level === 3,
      }">
      {{ title }}
      <span v-if="highlight" class="base-heading__highlight">{{ highlight }}</span>
    </component>

    <div v-if="$slots.default" class="base-heading__lead">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.base-heading {
  display: flex;
  flex-direction: column;
  gap: var(--space-sm);
  max-width: 44rem;
}

.base-heading--center {
  align-items: center;
  margin: 0 auto;
  text-align: center;
}

.base-heading__eyebrow {
  display: inline-flex;
  align-items: center;
  gap: var(--space-xs);
  color: var(--color-sage);
  font-size: var(--text-xs);
  font-weight: var(--weight-bold);
  letter-spacing: var(--tracking-label);
  text-transform: uppercase;
}

/* Short terracotta dash before the eyebrow */
.base-heading__eyebrow::before {
  width: 1.5rem;
  height: 2px;
  background: var(--color-terracotta);
  content: '';
}

.base-heading__title {
  color: var(--color-ink);
}

.base-heading__title--h1 {
  font-size: var(--text-2xl);
}

.base-heading__title--h2 {
  font-size: var(--text-xl);
}

.base-heading__title--h3 {
  font-size: var(--text-lg);
}

.base-heading__highlight {
  display: block;
  color: var(--color-blue);
}

.base-heading__lead {
  max-width: 36rem;
  color: var(--color-ink);
  font-size: var(--text-md);
}

.base-heading--inverse .base-heading__title {
  color: var(--color-white);
}

.base-heading--inverse .base-heading__highlight {
  color: var(--color-terracotta);
}

.base-heading--inverse .base-heading__lead {
  color: var(--color-white);
}
</style>
