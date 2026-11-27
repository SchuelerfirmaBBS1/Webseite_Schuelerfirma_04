<script setup lang="ts">
import { computed, provide, useId } from 'vue'
import { fieldContextKey } from './fieldContext'

/**
 * Wraps one form control with its label, hint and error message.
 * The control inside (BaseInput, BaseTextarea, BaseSelect) picks up id and aria wiring automatically.
 */
const props = defineProps<{
  label: string
  /** Short help text below the label */
  hint?: string
  /** Validation message; marks the control as invalid */
  error?: string
  required?: boolean
}>()

const id = useId()
const hintId = `${id}-hint`
const errorId = `${id}-error`

const describedBy = computed(() => {
  const ids = []
  if (props.hint) ids.push(hintId)
  if (props.error) ids.push(errorId)
  return ids.length ? ids.join(' ') : undefined
})

provide(fieldContextKey, {
  id,
  describedBy,
  invalid: computed(() => Boolean(props.error)),
  required: computed(() => props.required),
})
</script>

<template>
  <div class="base-field" :class="{ 'has-error': error }">
    <label :for="id" class="base-field__label">
      {{ label }}
      <span v-if="required" class="base-field__required" aria-hidden="true">*</span>
    </label>

    <p v-if="hint" :id="hintId" class="base-field__hint">{{ hint }}</p>

    <slot />

    <p v-if="error" :id="errorId" class="base-field__error" role="alert">{{ error }}</p>
  </div>
</template>

<style scoped>
.base-field {
  display: flex;
  flex-direction: column;
  gap: var(--space-xs);
}

.base-field__label {
  color: var(--color-ink);
  font-size: var(--text-sm);
  font-weight: var(--weight-bold);
}

.base-field__required {
  color: var(--color-ink);
}

.base-field__hint {
  color: var(--color-ink);
  font-size: var(--text-xs);
}

.base-field__error {
  color: var(--color-error);
  font-size: var(--text-sm);
}
</style>
