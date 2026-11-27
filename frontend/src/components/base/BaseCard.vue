<script setup lang="ts">
/**
 * Content card: optional media on top (slot `media`), a tag, title, text (default slot)
 * and an optional footer (slot `footer`, e.g. a text BaseButton).
 */
withDefaults(
  defineProps<{
    title: string
    /** Small label above the title */
    tag?: string
    /** Heading level inside the page outline */
    level?: 2 | 3 | 4
    /** Lift with a hard shadow on hover; use when the whole card is a link target */
    interactive?: boolean
  }>(),
  { level: 3, interactive: false },
)
</script>

<template>
  <article class="base-card" :class="{ 'base-card--interactive': interactive }">
    <div v-if="$slots.media" class="base-card__media">
      <slot name="media" />
    </div>

    <div class="base-card__body">
      <p v-if="tag" class="base-card__tag">{{ tag }}</p>
      <component :is="`h${level}`" class="base-card__title">{{ title }}</component>
      <div v-if="$slots.default" class="base-card__text">
        <slot />
      </div>
    </div>

    <footer v-if="$slots.footer" class="base-card__footer">
      <slot name="footer" />
    </footer>
  </article>
</template>

<style scoped>
.base-card {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border: var(--border);
  border-radius: var(--radius-organic);
  background: var(--color-white);
  transition:
    transform var(--duration-normal) var(--ease-out),
    box-shadow var(--duration-normal) var(--ease-out);
}

.base-card--interactive:hover {
  transform: translate(-4px, -4px);
  box-shadow: var(--shadow-offset) var(--shadow-color);
}

.base-card__media {
  border-bottom: var(--border);
}

/* Media in a card fills the top edge without its own frame */
.base-card__media :deep(.base-image) {
  border-radius: 0;
  box-shadow: none;
}

.base-card__body {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: var(--space-xs);
  padding: var(--space-md);
}

.base-card__tag {
  color: var(--color-sage);
  font-size: var(--text-xs);
  font-weight: var(--weight-bold);
  letter-spacing: var(--tracking-label);
  text-transform: uppercase;
}

.base-card__title {
  font-size: var(--text-lg);
}

.base-card__text {
  color: var(--color-ink);
  font-size: var(--text-sm);
}

.base-card__footer {
  padding: 0 var(--space-md) var(--space-md);
}
</style>
