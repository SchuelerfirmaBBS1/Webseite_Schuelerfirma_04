<script setup lang="ts">
/**
 * Image and text side by side (stacked on small screens).
 * Slots: `media` (e.g. a BaseImage, optionally with a BaseBlob behind it) and default (text).
 */
withDefaults(
  defineProps<{
    /** Put the image on the right instead of the left */
    reverse?: boolean
  }>(),
  { reverse: false },
)
</script>

<template>
  <div class="base-media-text" :class="{ 'base-media-text--reverse': reverse }">
    <div class="base-media-text__media">
      <slot name="media" />
    </div>
    <div class="base-media-text__text">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.base-media-text {
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-items: center;
  gap: var(--space-xl);
}

.base-media-text--reverse .base-media-text__media {
  order: 2;
}

.base-media-text__media {
  position: relative;
}

.base-media-text__text {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-md);
}

@media (max-width: 800px) {
  .base-media-text {
    grid-template-columns: 1fr;
    gap: var(--space-lg);
  }

  .base-media-text--reverse .base-media-text__media {
    order: 0;
  }
}
</style>
