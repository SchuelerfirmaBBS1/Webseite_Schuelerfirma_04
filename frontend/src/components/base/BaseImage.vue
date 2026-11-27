<script setup lang="ts">
/**
 * Image with the site's frame styles. Without `src` it shows a neutral placeholder,
 * useful while real photos are missing.
 */
withDefaults(
  defineProps<{
    src?: string
    /** Required for real images; describe what is shown */
    alt?: string
    /** e.g. '4 / 3', '1 / 1', '16 / 9' */
    ratio?: string
    /** offset: hard shadow (blue, terracotta on dark sections); organic: asymmetric corners; plain: none */
    frame?: 'offset' | 'organic' | 'plain'
  }>(),
  { alt: '', ratio: '4 / 3', frame: 'offset' },
)
</script>

<template>
  <figure class="base-image" :class="`base-image--${frame}`" :style="{ aspectRatio: ratio }">
    <img v-if="src" :src="src" :alt="alt" class="base-image__img" loading="lazy" />
    <div v-else class="base-image__placeholder" role="img" :aria-label="alt || 'Bildplatzhalter'">
      <span aria-hidden="true">Bild</span>
    </div>
    <figcaption v-if="$slots.caption" class="base-image__caption"><slot name="caption" /></figcaption>
  </figure>
</template>

<style scoped>
.base-image {
  position: relative;
  margin: 0;
  border-radius: var(--radius-md);
}

.base-image__img,
.base-image__placeholder {
  width: 100%;
  height: 100%;
  border-radius: inherit;
  object-fit: cover;
}

.base-image__placeholder {
  display: grid;
  place-items: center;
  border: 2px dashed var(--color-grey);
  background: var(--color-cream);
  color: var(--color-grey);
  font-size: var(--text-sm);
  font-weight: var(--weight-bold);
  letter-spacing: var(--tracking-label);
  text-transform: uppercase;
}

.base-image--offset {
  box-shadow: var(--shadow-offset) var(--shadow-color);
}

.base-image--organic {
  border-radius: var(--radius-organic);
}

.base-image--plain {
  border-radius: var(--radius-sm);
}

.base-image__caption {
  position: absolute;
  bottom: var(--space-xs);
  left: var(--space-xs);
  padding: 0 var(--space-xs);
  border-radius: var(--radius-pill);
  background: var(--color-white);
  color: var(--color-ink);
  font-size: var(--text-xs);
  font-weight: var(--weight-bold);
  line-height: 1.8;
}
</style>
