<script setup lang="ts">
/**
 * Full-width page section with the shared content container.
 * Every page is built from a stack of these.
 */
withDefaults(
  defineProps<{
    /** Background: light (white), cream (page default), dark (ink, inverted text) */
    tone?: 'light' | 'cream' | 'dark'
    /** Vertical padding */
    spacing?: 'md' | 'lg'
    /** Rendered element, e.g. 'header' for a page head */
    as?: 'section' | 'header' | 'div'
  }>(),
  { tone: 'cream', spacing: 'lg', as: 'section' },
)
</script>

<template>
  <component
    :is="as"
    class="base-section"
    :class="{
      'base-section--light': tone === 'light',
      'base-section--cream': tone === 'cream',
      'base-section--dark': tone === 'dark',
      'base-section--md': spacing === 'md',
      'base-section--lg': spacing === 'lg',
    }"
  >
    <div class="base-section__inner">
      <slot />
    </div>
  </component>
</template>

<style scoped>
.base-section {
  position: relative;
  overflow: hidden;
}

.base-section--light {
  background: var(--color-white);
}

.base-section--cream {
  background: var(--color-cream);
}

/* Dark surface: inverted text and surface colours, shadows switch to terracotta */
.base-section--dark {
  --shadow-color: var(--color-terracotta);
  --color-fg: var(--color-white);
  --color-bg: var(--color-ink);

  background: var(--color-ink);
  color: var(--color-white);
}

.base-section--md {
  padding: var(--space-lg) 0;
}

.base-section--lg {
  padding: var(--space-xl) 0;
}

.base-section__inner {
  position: relative;
  width: 100%;
  max-width: var(--container-width);
  margin: 0 auto;
  padding: 0 var(--container-padding);
}
</style>
