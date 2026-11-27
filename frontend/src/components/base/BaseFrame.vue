<script setup lang="ts">
/**
 * Playful photo frame from the front page hero: the photo sits in a white passe-partout,
 * a coloured plate is rotated the other way behind it, and the whole frame is tilted.
 * Rules: only for the hero or image collages; never in cards, galleries or BaseMediaText
 * (use BaseImage there, in media-text with a BaseBlob behind it); at most one plate colour per frame, from the core palette.
 */
withDefaults(
  defineProps<{
    src: string
    /** Describe what is shown; empty only for purely decorative photos */
    alt: string
    /** arch: rounded top (portrait); round: circle on a blob plate */
    shape?: 'arch' | 'round'
    /** Colour of the plate behind the photo */
    plate?: 'blue' | 'terracotta' | 'ink' | 'sage'
    /** Direction of the tilt; alternate when two frames are near each other */
    tilt?: 'left' | 'right'
    /** Short pill label on the lower left corner, e.g. "Print" */
    label?: string
    labelTone?: 'cream' | 'terracotta'
  }>(),
  { shape: 'arch', plate: 'blue', tilt: 'left', labelTone: 'cream' },
)
</script>

<template>
  <figure
    class="base-frame"
    :class="{
      'base-frame--arch': shape === 'arch',
      'base-frame--round': shape === 'round',
      'base-frame--plate-blue': plate === 'blue',
      'base-frame--plate-terracotta': plate === 'terracotta',
      'base-frame--plate-ink': plate === 'ink',
      'base-frame--plate-sage': plate === 'sage',
      'base-frame--tilt-right': tilt === 'right',
    }"
  >
    <span class="base-frame__plate" aria-hidden="true"></span>
    <img :src="src" :alt="alt" class="base-frame__img" loading="lazy" />
    <figcaption v-if="label" class="base-frame__label" :class="{
        'base-frame__label--cream': labelTone === 'cream',
        'base-frame__label--terracotta': labelTone === 'terracotta',
      }">
      {{ label }}
    </figcaption>
  </figure>
</template>

<style scoped>
.base-frame {
  --frame-tilt: -6deg;

  position: relative;
  isolation: isolate;
  width: 100%;
  margin: 0;
  transform: rotate(var(--frame-tilt));
}

.base-frame--tilt-right {
  --frame-tilt: 6deg;
}

/* The plate is a little larger than the photo and turned against the tilt */
.base-frame__plate {
  position: absolute;
  z-index: -1;
  inset: calc(var(--space-xs) * -1.5);
  background: var(--plate-color);
  transform: rotate(calc(var(--frame-tilt) * -0.8));
}

.base-frame__img {
  display: block;
  width: 100%;
  height: 100%;
  border: var(--border-frame);
  background: var(--color-white);
  object-fit: cover;
  transition: transform var(--duration-normal) var(--ease-out);
}

.base-frame:hover .base-frame__img {
  transform: rotate(calc(var(--frame-tilt) * -0.5)) scale(1.03);
}

/* Shapes */
.base-frame--arch {
  aspect-ratio: 4 / 5;
}

.base-frame--arch .base-frame__img,
.base-frame--arch .base-frame__plate {
  border-radius: var(--radius-arch);
}

.base-frame--round {
  aspect-ratio: 1 / 1;
}

.base-frame--round .base-frame__img {
  border-radius: 50%;
}

.base-frame--round .base-frame__plate {
  border-radius: var(--radius-blob);
}

/* Plate colours (core palette only) */
.base-frame--plate-blue {
  --plate-color: var(--color-blue);
}

.base-frame--plate-terracotta {
  --plate-color: var(--color-terracotta);
}

.base-frame--plate-ink {
  --plate-color: var(--color-ink);
}

.base-frame--plate-sage {
  --plate-color: var(--color-sage);
}

/* Label pill, overlapping the lower left corner and set straight against the tilt */
.base-frame__label {
  position: absolute;
  bottom: var(--space-sm);
  left: calc(var(--space-sm) * -1);
  padding: 0 var(--space-sm);
  border: var(--border);
  border-radius: var(--radius-pill);
  box-shadow: var(--shadow-offset-pressed) var(--shadow-color);
  color: var(--color-ink);
  font-size: var(--text-xs);
  font-weight: var(--weight-bold);
  letter-spacing: var(--tracking-label);
  line-height: 2.2;
  text-transform: uppercase;
  transform: rotate(calc(var(--frame-tilt) * -1));
}

.base-frame__label--cream {
  background: var(--color-cream);
}

.base-frame__label--terracotta {
  background: var(--color-terracotta);
}
</style>
