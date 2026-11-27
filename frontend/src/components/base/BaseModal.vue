<script setup lang="ts">
import { onMounted, ref, useId, watch } from 'vue'

/**
 * Modal dialog on top of the native <dialog>: the browser keeps focus inside,
 * Esc closes it, and the page behind becomes inert.
 * Open and close it with v-model:open.
 */
withDefaults(
  defineProps<{
    title: string
    /** Small label above the title */
    eyebrow?: string
    size?: 'sm' | 'md'
  }>(),
  { size: 'md' },
)

const open = defineModel<boolean>('open', { default: false })

const dialog = ref<HTMLDialogElement | null>(null)
const titleId = useId()

function sync(isOpen: boolean) {
  const el = dialog.value
  if (!el) return
  if (isOpen && !el.open) el.showModal()
  if (!isOpen && el.open) el.close()
}

watch(open, sync)
onMounted(() => sync(open.value))

/** Native close (Esc key or dialog.close()) */
function onClose() {
  open.value = false
}

/** A click on the dialog element itself (not its content) is a click on the backdrop */
function onClick(event: MouseEvent) {
  if (event.target === dialog.value) open.value = false
}
</script>

<template>
  <dialog
    ref="dialog"
    class="base-modal"
    :class="`base-modal--${size}`"
    :aria-labelledby="titleId"
    @close="onClose"
    @click="onClick"
  >
    <div class="base-modal__panel">
      <header class="base-modal__header">
        <div>
          <p v-if="eyebrow" class="base-modal__eyebrow">{{ eyebrow }}</p>
          <h2 :id="titleId" class="base-modal__title">{{ title }}</h2>
        </div>
        <button type="button" class="base-modal__close" aria-label="Schließen" @click="open = false">
          ×
        </button>
      </header>

      <div class="base-modal__body">
        <slot />
      </div>

      <footer v-if="$slots.actions" class="base-modal__actions">
        <slot name="actions" />
      </footer>
    </div>
  </dialog>
</template>

<style scoped>
.base-modal {
  width: min(100% - 2 * var(--space-sm), 36rem);
  max-height: min(90vh, 48rem);
  padding: 0;
  border: var(--border);
  border-radius: var(--radius-organic);
  background: var(--color-white);
  color: var(--color-ink);
  box-shadow: var(--shadow-offset) var(--shadow-color);
}

.base-modal--sm {
  width: min(100% - 2 * var(--space-sm), 26rem);
}

.base-modal::backdrop {
  background: color-mix(in srgb, var(--color-ink) 55%, transparent);
}

.base-modal[open] {
  animation: base-modal-in var(--duration-normal) var(--ease-out);
}

.base-modal__panel {
  display: flex;
  flex-direction: column;
  gap: var(--space-md);
  padding: var(--space-lg);
}

.base-modal__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-sm);
}

.base-modal__eyebrow {
  margin-bottom: var(--space-xs);
  color: var(--color-sage);
  font-size: var(--text-xs);
  font-weight: var(--weight-bold);
  letter-spacing: var(--tracking-label);
  text-transform: uppercase;
}

.base-modal__title {
  font-size: var(--text-xl);
}

.base-modal__close {
  flex-shrink: 0;
  display: grid;
  place-items: center;
  width: 2.5rem;
  height: 2.5rem;
  border: var(--border);
  border-radius: 50%;
  background: transparent;
  font-size: 1.5rem;
  line-height: 1;
  cursor: pointer;
  transition: background-color var(--duration-fast) var(--ease-out);
}

.base-modal__close:hover {
  background: var(--color-cream);
}

.base-modal__body {
  color: var(--color-ink);
}

.base-modal__actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: var(--space-sm);
}

@keyframes base-modal-in {
  from {
    opacity: 0;
    transform: translateY(12px) scale(0.98);
  }
}

@media (max-width: 480px) {
  .base-modal__panel {
    padding: var(--space-md);
  }
}
</style>
