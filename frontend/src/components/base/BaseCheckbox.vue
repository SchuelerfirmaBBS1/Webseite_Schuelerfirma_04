<script setup lang="ts">
import { useId } from 'vue'

/** Checkbox with its label to the right, e.g. for a consent line. The label text goes in the default slot. */
defineProps<{ required?: boolean; disabled?: boolean }>()

const model = defineModel<boolean>({ default: false })
const id = useId()
</script>

<template>
  <div class="base-checkbox">
    <input
      :id="id"
      v-model="model"
      type="checkbox"
      class="base-checkbox__input"
      :required="required"
      :disabled="disabled"
    />
    <label :for="id" class="base-checkbox__label"><slot /></label>
  </div>
</template>

<style scoped>
.base-checkbox {
  display: flex;
  align-items: flex-start;
  gap: var(--space-xs);
}

.base-checkbox__input {
  flex-shrink: 0;
  display: grid;
  place-content: center;
  width: 1.4rem;
  height: 1.4rem;
  margin: 0.15rem 0 0;
  border: 2px solid var(--color-ink);
  border-radius: var(--radius-sm);
  background: var(--color-white);
  appearance: none;
  cursor: pointer;
  transition: background-color var(--duration-fast) var(--ease-out);
}

.base-checkbox__input::after {
  width: 0.7rem;
  height: 0.4rem;
  border-bottom: 2px solid var(--color-white);
  border-left: 2px solid var(--color-white);
  content: '';
  transform: rotate(-45deg) scale(0);
  transition: transform var(--duration-fast) var(--ease-out);
}

.base-checkbox__input:checked {
  background: var(--color-blue);
  border-color: var(--color-blue);
}

.base-checkbox__input:checked::after {
  transform: translateY(-1px) rotate(-45deg) scale(1);
}

.base-checkbox__input:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.base-checkbox__label {
  color: var(--color-ink);
  font-size: var(--text-sm);
  line-height: var(--leading-body);
  cursor: pointer;
}
</style>
