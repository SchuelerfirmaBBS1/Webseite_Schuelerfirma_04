<script setup lang="ts">
import { useFieldControl } from './fieldContext'

export interface SelectOption {
  value: string
  label: string
}

/** Dropdown. Use inside a BaseField for label and error handling. */
defineProps<{
  options: SelectOption[]
  /** Shown as an empty first choice */
  placeholder?: string
}>()

const model = defineModel<string>({ default: '' })
const fieldAttrs = useFieldControl()
</script>

<template>
  <select v-model="model" class="form-control" v-bind="fieldAttrs">
    <option v-if="placeholder" value="" disabled>{{ placeholder }}</option>
    <option v-for="option in options" :key="option.value" :value="option.value">
      {{ option.label }}
    </option>
  </select>
</template>

<style scoped src="../../assets/css/form-control.css"></style>

<style scoped>
.form-control {
  padding-right: var(--space-lg);
  appearance: none;
  background-image:
    linear-gradient(45deg, transparent 50%, var(--color-ink) 50%),
    linear-gradient(135deg, var(--color-ink) 50%, transparent 50%);
  background-position:
    right 1.4rem center,
    right 1rem center;
  background-size: 0.4rem 0.4rem;
  background-repeat: no-repeat;
  cursor: pointer;
}
</style>
